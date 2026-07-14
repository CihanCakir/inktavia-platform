# Claude Code Prompt — Prove the SignalR Redis backplane with two BFF replicas

This is a **verification** task, not a feature. Write as little code as possible. The goal is to produce evidence,
and the evidence must be able to come back negative.

## Why

`docs/realtime-kubernetes-report.md` enabled the Redis backplane on four hubs and reported the two-replica test as
an open item. **So the backplane has never carried a single message between two processes.** The configuration
looks right, the code looks right — and that is exactly what the failure mode looks like too. In Kubernetes the
services run with many replicas; without a working backplane a provider's socket sits on pod A while the RabbitMQ
message is consumed by pod B, and the event is **silently dropped**: no error, no retry, nothing to grep. It passes
every single-pod test and then loses most events in production.

Until this test runs, "the backplane works" is a guess.

## Setup (already in the repo)

`docker-compose.yaml` now defines **`bff-marineprovider-2`** — a second instance of the BFF: same image, same env,
same Redis, same RabbitMQ. Only the container name and the host port differ (17012 vs 17002). It exists purely for
this test; in Kubernetes it would just be `replicas: 2`.

```bash
docker compose up -d --build bff-marineprovider bff-marineprovider-2
```

Both instances consume from RabbitMQ as **competing consumers**: each published message is delivered to exactly
one of them.

## Precondition — verify the socket exists BEFORE you measure anything

**Do not skip this.** A first attempt at this test was run while the browser could not connect to the hub at all:
`/hubs/provider/negotiate` was answering **401 on the CORS preflight** (the BFF called `UseCors()` after
`Build()`, which put the CORS middleware *behind* `UseAuthorization()`; an `OPTIONS` carries no `Authorization`
header, so the preflight was rejected and the browser abandoned the connection). Every event would have been
counted as "did not arrive" — and the backplane would have taken the blame for a CORS bug. That is now fixed in
`AizenBffApplicationConfiguration`.

Zero arrivals is consistent with **both** "the backplane is broken" and "there was never a connection". The test
cannot tell them apart, so establish the connection first:

- `POST /hubs/provider/negotiate` returns **200** (not 401, not 503);
- instance 1's log shows the connection and **which group it joined**;
- only then start publishing.

If the socket is not up, stop and fix that. Do not publish into a void and report the silence as a result.

## The test

A provider (`provider2@inktavia.com`, profile id 100011) is connected through **instance 1 only** (the browser
talks to port 17002). Instance 2 holds **no socket at all**.

1. **Publish ~6 service requests in the provider's own city**, one at a time, a few seconds apart, each with a
   distinguishable title (`BACKPLANE-TEST-1` … `-6`).

   **Read the city from provider profile 100011 — do not assume it.** The hub group is `city:{the provider's city
   code}`. Publish into a different city and nothing arrives, correctly, and you will have measured the city
   filter instead of the backplane. Confirm the city code you publish with is the same one the connection joined.

   They must travel the **real path**: the `PublishServiceRequest` command → `ServiceRequestPublishedMessage` on
   RabbitMQ → a BFF instance consumes it → SignalR. A request inserted straight into Postgres publishes nothing and
   proves nothing.

   How you drive that is your call — you have shell access, the compose network, `.env` (Keycloak client secrets),
   `kcadm`, and `docker exec`. Options, in the order I would try them:
   - drive the ServiceRequest module API from inside the compose network with a valid token (work out the audience
     and the BFF assertion headers from `MarineProviderBffAuthDelegatingHandler` and the module's auth setup);
   - or publish the message onto RabbitMQ directly — but **only** if you first capture a real
     `ServiceRequestPublishedMessage` off the wire and reproduce its exact envelope. A hand-invented envelope that
     the consumer ignores would look like a backplane failure and send us chasing the wrong thing.

   If you fabricate the transport, say so loudly. A synthetic message that never went through the command handler
   does not test the same thing.

2. **Record, per request, which instance consumed it.** Both instances log
   `Pushed ServiceRequestPublished {id} to group city:{code}`. Tail both:
   ```bash
   docker logs -f bff-marineprovider   | grep -i "Pushed ServiceRequestPublished"
   docker logs -f bff-marineprovider-2 | grep -i "Pushed ServiceRequestPublished"
   ```
   Expect the six to be split roughly 50/50 across the two. If they all land on one instance, the consumers are
   **not** competing — that is itself an important finding; report it and stop, because the test is meaningless
   until it is understood.

3. **Report which of the six reached the browser.** The frontend surfaces each as a toast; the human driving the
   browser will tell you. Correlate: request → consuming instance → arrived?

## The two runs — the negative one is the point

**Run A — backplane OFF.** Remove `Realtime__SignalR__RedisConnectionString` from **both** BFF instances, restart,
repeat. Expected: only the requests consumed by **instance 1** arrive. The ones consumed by instance 2 vanish —
while instance 2's log still says it "pushed" them, because it did, into its own empty hub.

**Run B — backplane ON.** Restore the config, restart, repeat. Expected: **all six arrive**, regardless of which
instance consumed them.

If run A does not lose events, the test is not proving what we think it is — say so instead of declaring success.
Two green runs mean the test is broken, not that we are safe.

## Report

Write `docs/realtime-backplane-two-replica-report.md` containing:

- exactly how the requests were published (and whether it was the real command path);
- a table: request → consuming instance → arrived in the browser (run A / run B);
- the consumer model, stated as a fact you observed: one replica per message, or all replicas;
- whether `IConnectionMultiplexer` / the Redis backplane actually connected (check the logs and `redis-cli` on
  DB 15 — `PUBSUB CHANNELS` should show SignalR channels while a client is connected);
- anything that surprised you.

**Report failures plainly.** If run A cannot be made to lose events, or you could not publish through the real
path, say so. An unverified backplane described as verified is worse than a known gap — this codebase has already
shipped several of those.

## Constraints

- Do not change application behaviour to make the test pass. Config only.
- Restore the compose file to the backplane-ON state when you are done.
- No fabricated evidence. If a step could not be run, it is "not run", not "passed".
