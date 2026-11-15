#!/bin/sh
# Runs the migrations job azd has just deployed and waits for it, so the host azd deploys next starts on a migrated
# database (the AppHost's WaitForCompletion(migrations)). azd runs it after deploying the migrations service, with the
# environment's values (AZURE_RESOURCE_GROUP, MIGRATIONS_JOB_NAME) set.
set -eu

: "${AZURE_RESOURCE_GROUP:?run through azd, which sets it}"
: "${MIGRATIONS_JOB_NAME:?run through azd, which sets it}"

execution=$(az containerapp job start --name "$MIGRATIONS_JOB_NAME" --resource-group "$AZURE_RESOURCE_GROUP" --query name --output tsv)
echo "Migrations: execution $execution started."

attempts=0
while :; do
  status=$(az containerapp job execution show --name "$MIGRATIONS_JOB_NAME" --resource-group "$AZURE_RESOURCE_GROUP" \
    --job-execution-name "$execution" --query properties.status --output tsv)
  case "$status" in
    Succeeded)
      echo "Migrations: $status."
      exit 0
      ;;
    Failed | Stopped | Degraded)
      echo "Migrations: $status. The job's logs are in the Log Analytics workspace (ContainerAppConsoleLogs_CL, ContainerJobName_s == \"$MIGRATIONS_JOB_NAME\")." >&2
      exit 1
      ;;
  esac
  attempts=$((attempts + 1))
  if [ "$attempts" -ge 180 ]; then
    echo "Migrations: still $status after 30 minutes; giving up waiting." >&2
    exit 1
  fi
  sleep 10
done
