FROM tianon/true

COPY components/email.yaml.compose /components/email.yaml
COPY components/statestore.yaml.compose /components/statestore.yaml
COPY components/secrets-file.yaml.compose /components/secrets-file.yaml

COPY components/entrycam.yaml /components
COPY components/exitcam.yaml /components
COPY components/pubsub.yaml /components
COPY components/secrets-envvars.yaml /components
COPY components/secrets-keyvault.yaml /components
COPY components/secrets.json /components

COPY config/config.yaml.compose /config/config.yaml
COPY config/ratelimit-config.yaml.compose /config/ratelimit-config.yaml

VOLUME /components
VOLUME /config