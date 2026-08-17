#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/provider-otp-login-authenticator"

echo "Building Keycloak SPI JAR with Maven (Docker)..."
docker run --rm -v "$(pwd)":/build -w /build maven:3.9-eclipse-temurin-17 mvn clean package -q -DskipTests

echo "Copying JAR to providers directory..."
mkdir -p ../../providers
cp target/provider-otp-login-authenticator.jar ../../providers/

echo "Done: infrastructure/keycloak/providers/provider-otp-login-authenticator.jar"
