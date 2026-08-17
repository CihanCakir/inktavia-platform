package com.inktavia.keycloak.authenticator;

import org.keycloak.Config;
import org.keycloak.authentication.Authenticator;
import org.keycloak.authentication.AuthenticatorFactory;
import org.keycloak.models.AuthenticationExecutionModel;
import org.keycloak.models.KeycloakSession;
import org.keycloak.models.KeycloakSessionFactory;
import org.keycloak.provider.ProviderConfigProperty;

import java.util.List;

public class ProviderOtpLoginAuthenticatorFactory implements AuthenticatorFactory {

    public static final String PROVIDER_ID = "provider-otp-login-ticket";

    private static final List<ProviderConfigProperty> CONFIG_PROPERTIES = List.of(
        new ProviderConfigProperty("identityBaseUrl", "Identity Base URL",
            "Base URL of the Identity API (e.g. http://identity-api:8080)", ProviderConfigProperty.STRING_TYPE, "http://identity-api:8080"),
        new ProviderConfigProperty("consumePath", "Consume Ticket Path",
            "Path for the consume-ticket endpoint", ProviderConfigProperty.STRING_TYPE, "/api/v1/identity/auth/provider-otp-login/consume-ticket"),
        new ProviderConfigProperty("ticketSecret", "Ticket Secret (HMAC)",
            "Shared HMAC-SHA256 secret for ticket verification", ProviderConfigProperty.PASSWORD, ""),
        new ProviderConfigProperty("consumeSecret", "Consume Secret",
            "Secret sent as X-Otp-Login-Consume-Secret header", ProviderConfigProperty.PASSWORD, ""),
        new ProviderConfigProperty("allowedClientId", "Allowed Client ID",
            "Client ID the ticket must be bound to", ProviderConfigProperty.STRING_TYPE, "provider-portal"),
        new ProviderConfigProperty("maxSkewSeconds", "Max Clock Skew (seconds)",
            "Maximum allowed clock skew for ticket expiration", ProviderConfigProperty.STRING_TYPE, "30")
    );

    @Override
    public Authenticator create(KeycloakSession session) {
        return new ProviderOtpLoginAuthenticator();
    }

    @Override public String getId() { return PROVIDER_ID; }
    @Override public String getDisplayType() { return "Provider OTP Login Ticket"; }
    @Override public String getReferenceCategory() { return "otp-login"; }
    @Override public String getHelpText() { return "Validates a single-use HMAC-signed login ticket from the Identity OTP-login flow."; }
    @Override public boolean isConfigurable() { return true; }
    @Override public boolean isUserSetupAllowed() { return false; }
    @Override public List<ProviderConfigProperty> getConfigProperties() { return CONFIG_PROPERTIES; }
    @Override public AuthenticationExecutionModel.Requirement[] getRequirementChoices() {
        return new AuthenticationExecutionModel.Requirement[]{ AuthenticationExecutionModel.Requirement.ALTERNATIVE, AuthenticationExecutionModel.Requirement.DISABLED };
    }
    @Override public void init(Config.Scope config) {}
    @Override public void postInit(KeycloakSessionFactory factory) {}
    @Override public void close() {}
}
