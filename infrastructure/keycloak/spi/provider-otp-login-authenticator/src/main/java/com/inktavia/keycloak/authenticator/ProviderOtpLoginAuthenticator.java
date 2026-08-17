package com.inktavia.keycloak.authenticator;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.jboss.logging.Logger;
import org.keycloak.authentication.AuthenticationFlowContext;
import org.keycloak.authentication.AuthenticationFlowError;
import org.keycloak.authentication.Authenticator;
import org.keycloak.models.KeycloakSession;
import org.keycloak.models.RealmModel;
import org.keycloak.models.UserModel;
import org.keycloak.sessions.AuthenticationSessionModel;

import javax.crypto.Mac;
import javax.crypto.spec.SecretKeySpec;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URI;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.time.Instant;
import java.util.Base64;
import java.util.Map;

/**
 * Logs a provider in when the authorize request carries a valid, single-use HMAC login_ticket
 * minted by the Identity module after a successful OTP verification.
 *
 * Fallback contract: when no ticket is present, or the ticket is unusable, this authenticator calls
 * context.attempted() so the flow falls through to the normal (password / IdP) login. It NEVER fakes
 * a login — the Keycloak session is only issued via context.setUser + context.success.
 *
 * Every decision point is logged so failures are diagnosable (previously this class logged nothing,
 * which made a silent fall-through indistinguishable from the authenticator not running at all).
 */
public class ProviderOtpLoginAuthenticator implements Authenticator {

    private static final Logger LOG = Logger.getLogger(ProviderOtpLoginAuthenticator.class);
    private static final ObjectMapper MAPPER = new ObjectMapper();
    private static final String TICKET_PARAM = "login_ticket";

    @Override
    public void authenticate(AuthenticationFlowContext context) {
        AuthenticationSessionModel session = context.getAuthenticationSession();

        String ticket = resolveTicket(context, session);

        if (ticket == null || ticket.isBlank()) {
            LOG.debugf("[otp-login] No %s on the authorize request (uri=%s) — falling through to normal login.",
                    TICKET_PARAM, safeUri(context));
            context.attempted();
            return;
        }

        // Persist for the rest of the flow: Keycloak may re-enter the flow on a URL that no longer
        // carries the original authorize query string.
        session.setClientNote(TICKET_PARAM, ticket);

        var config = context.getAuthenticatorConfig();
        if (config == null || config.getConfig() == null) {
            LOG.warn("[otp-login] Authenticator has no config (ticketSecret/consumeSecret missing) — falling through.");
            context.attempted();
            return;
        }
        Map<String, String> cfg = config.getConfig();

        try {
            // 1. Split ticket: base64url(payload) '.' base64url(hmac)
            int dot = ticket.indexOf('.');
            if (dot <= 0 || dot >= ticket.length() - 1) {
                LOG.warnf("[otp-login] Malformed ticket (no separator, len=%d).", ticket.length());
                context.failure(AuthenticationFlowError.INVALID_CREDENTIALS);
                return;
            }
            byte[] payloadBytes = base64UrlDecode(ticket.substring(0, dot));
            byte[] receivedSig = base64UrlDecode(ticket.substring(dot + 1));

            // 2. Verify HMAC-SHA256
            String secret = cfg.getOrDefault("ticketSecret", "");
            if (secret.isBlank()) {
                LOG.warn("[otp-login] ticketSecret is empty in the authenticator config — cannot verify signature.");
                context.failure(AuthenticationFlowError.INVALID_CREDENTIALS);
                return;
            }
            Mac mac = Mac.getInstance("HmacSHA256");
            mac.init(new SecretKeySpec(secret.getBytes(StandardCharsets.UTF_8), "HmacSHA256"));
            byte[] expectedSig = mac.doFinal(payloadBytes);

            if (!MessageDigest.isEqual(expectedSig, receivedSig)) {
                LOG.warn("[otp-login] Ticket HMAC signature mismatch — ticketSecret likely differs from Identity's.");
                context.failure(AuthenticationFlowError.INVALID_CREDENTIALS);
                return;
            }

            // 3. Parse payload
            JsonNode payload = MAPPER.readTree(payloadBytes);
            String sub = payload.path("sub").asText("");
            String clientId = payload.path("clientId").asText("");
            long exp = payload.path("exp").asLong(0);
            String jti = payload.path("jti").asText("");
            String nonce = payload.path("nonce").asText("");

            LOG.debugf("[otp-login] Ticket verified. sub=%s clientId=%s jti=%s exp=%d now=%d",
                    sub, clientId, jti, exp, Instant.now().getEpochSecond());

            // 4. Validate claims
            String allowedClientId = cfg.getOrDefault("allowedClientId", "provider-portal");
            int maxSkew = Integer.parseInt(cfg.getOrDefault("maxSkewSeconds", "30"));

            if (sub.isBlank() || jti.isBlank() || nonce.isBlank()) {
                LOG.warnf("[otp-login] Ticket missing required claims (sub/jti/nonce). sub='%s' jti='%s' nonce='%s'",
                        sub, jti, nonce);
                context.failure(AuthenticationFlowError.INVALID_CREDENTIALS);
                return;
            }
            if (!allowedClientId.equals(clientId)) {
                LOG.warnf("[otp-login] Ticket clientId '%s' != allowedClientId '%s'.", clientId, allowedClientId);
                context.failure(AuthenticationFlowError.INVALID_CREDENTIALS);
                return;
            }
            long now = Instant.now().getEpochSecond();
            if (now > exp + maxSkew) {
                LOG.warnf("[otp-login] Ticket expired. exp=%d now=%d maxSkew=%d", exp, now, maxSkew);
                context.failure(AuthenticationFlowError.EXPIRED_CODE);
                return;
            }

            // 5. Consume the ticket (single-use) via the Identity callback
            String identityBaseUrl = cfg.getOrDefault("identityBaseUrl", "http://identity-api:8080");
            String consumePath = cfg.getOrDefault("consumePath",
                    "/api/v1/identity/auth/provider-otp-login/consume-ticket");
            String consumeSecret = cfg.getOrDefault("consumeSecret", "");
            String consumeUrl = identityBaseUrl + consumePath;

            String consumedSub = consumeTicket(consumeUrl, jti, consumeSecret);
            if (consumedSub == null) {
                LOG.warnf("[otp-login] Consume failed (jti=%s, url=%s) — ticket already used, unknown, or Identity unreachable.",
                        jti, consumeUrl);
                context.failure(AuthenticationFlowError.INVALID_CREDENTIALS);
                return;
            }
            if (!sub.equals(consumedSub)) {
                LOG.warnf("[otp-login] Consume returned sub '%s' but ticket claims '%s'.", consumedSub, sub);
                context.failure(AuthenticationFlowError.INVALID_CREDENTIALS);
                return;
            }

            // 6. Resolve the Keycloak user
            RealmModel realm = context.getRealm();
            UserModel user = context.getSession().users().getUserById(realm, sub);
            if (user == null) {
                user = context.getSession().users().getUserByUsername(realm, sub);
            }
            if (user == null) {
                LOG.warnf("[otp-login] No Keycloak user for sub '%s' in realm '%s'.", sub, realm.getName());
                context.failure(AuthenticationFlowError.INVALID_USER);
                return;
            }
            if (!user.isEnabled()) {
                LOG.warnf("[otp-login] Keycloak user '%s' is disabled.", user.getUsername());
                context.failure(AuthenticationFlowError.INVALID_USER);
                return;
            }

            // 7. Success — Keycloak (and only Keycloak) issues the session/tokens
            LOG.infof("[otp-login] Ticket accepted — authenticating user '%s' (sub=%s).", user.getUsername(), sub);
            context.setUser(user);
            context.success();

        } catch (Exception e) {
            LOG.error("[otp-login] Unexpected error while processing the login ticket — falling through to normal login.", e);
            context.attempted();
        }
    }

    /** Ticket may arrive on the authorize query string, or (on flow re-entry) as a note on the auth session. */
    private String resolveTicket(AuthenticationFlowContext context, AuthenticationSessionModel session) {
        String ticket = null;
        try {
            ticket = context.getHttpRequest().getUri().getQueryParameters().getFirst(TICKET_PARAM);
        } catch (Exception e) {
            LOG.debugf("[otp-login] Could not read %s from the request URI: %s", TICKET_PARAM, e.getMessage());
        }
        if (isBlank(ticket)) ticket = session.getClientNote(TICKET_PARAM);
        if (isBlank(ticket)) ticket = session.getAuthNote(TICKET_PARAM);
        return ticket;
    }

    private String consumeTicket(String url, String jti, String consumeSecret) {
        HttpURLConnection conn = null;
        try {
            conn = (HttpURLConnection) URI.create(url).toURL().openConnection();
            conn.setRequestMethod("POST");
            conn.setRequestProperty("Content-Type", "application/json");
            conn.setRequestProperty("X-Otp-Login-Consume-Secret", consumeSecret);
            conn.setDoOutput(true);
            conn.setConnectTimeout(5000);
            conn.setReadTimeout(5000);

            String body = "{\"jti\":\"" + jti.replace("\"", "") + "\"}";
            try (OutputStream os = conn.getOutputStream()) {
                os.write(body.getBytes(StandardCharsets.UTF_8));
            }

            int status = conn.getResponseCode();
            if (status == 200) {
                String responseBody = new String(conn.getInputStream().readAllBytes(), StandardCharsets.UTF_8);
                JsonNode root = MAPPER.readTree(responseBody);
                // The endpoint returns a flat {consumed, sub}; tolerate an enveloped {body:{...}} too.
                JsonNode node = root.has("consumed") ? root : root.path("body");
                if (node.path("consumed").asBoolean(false)) {
                    return node.path("sub").asText(null);
                }
                LOG.warnf("[otp-login] Consume returned 200 but consumed=false. body=%s", responseBody);
                return null;
            }

            LOG.warnf("[otp-login] Consume HTTP %d from %s (401=secret mismatch, 410=already used/unknown jti).",
                    status, url);
            return null;
        } catch (Exception e) {
            LOG.errorf(e, "[otp-login] Consume call to %s failed.", url);
            return null;
        } finally {
            if (conn != null) conn.disconnect();
        }
    }

    private static boolean isBlank(String s) {
        return s == null || s.isBlank();
    }

    private static String safeUri(AuthenticationFlowContext context) {
        try {
            return String.valueOf(context.getHttpRequest().getUri().getRequestUri());
        } catch (Exception e) {
            return "<unavailable>";
        }
    }

    private static byte[] base64UrlDecode(String input) {
        String padded = input.replace('-', '+').replace('_', '/');
        switch (padded.length() % 4) {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Base64.getDecoder().decode(padded);
    }

    @Override public void action(AuthenticationFlowContext context) { context.attempted(); }
    @Override public boolean requiresUser() { return false; }
    @Override public boolean configuredFor(KeycloakSession session, RealmModel realm, UserModel user) { return true; }
    @Override public void setRequiredActions(KeycloakSession session, RealmModel realm, UserModel user) {}
    @Override public void close() {}
}
