## ADDED Requirements

### Requirement: Weather forecast endpoint requires authorization
The `GET /weatherforecast` endpoint in Weather.API SHALL require a valid authorization token via `.RequireAuthorization()`, so that unauthenticated callers that bypass the YARP gateway cannot access forecast data.

#### Scenario: Authenticated request succeeds
- **WHEN** a caller sends a `GET /weatherforecast` request with a valid bearer token
- **THEN** the response is HTTP 200 with weather forecast data

#### Scenario: Unauthenticated request is rejected
- **WHEN** a caller sends a `GET /weatherforecast` request without a bearer token or with an invalid token
- **THEN** the response is HTTP 401 Unauthorized

#### Scenario: Direct access to Weather API is protected
- **WHEN** a caller bypasses the YARP gateway and calls the Weather API port directly without a token
- **THEN** the response is HTTP 401 Unauthorized
