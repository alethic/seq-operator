#!/bin/bash
# Seq init script generates firstRun.adminPasswordHash from SEQ_FIRSTRUN_ADMINPASSWORD,
# just like the Seq deployment was given SEQ_FIRSTRUN_ADMINPASSWORDHASH instead.
# This hack is required in order to:
# 1. Avoid prompt for password change during initial login to Seq UI
# 2. Allow re-use of password from Kubernetes Secret by agents external to Seq,
#    like the seq-apikey-operator or other which need to generate dedicated
#    Seq API key in order to perform any operations with Seq CLI or API.
#
set -e
SCRIPT_NAME="$(basename "$0")"

LOGFILE="/var/log/seqinit/${SCRIPT_NAME%.sh}.log"
mkdir -p "$(dirname "${LOGFILE}")"

function echolog { echo "$(printf '%(%F %T)T') [${SCRIPT_NAME}] INFO $*" | tee -a "${LOGFILE}"; }
function echoerr { echo "$(printf '%(%F %T)T') [${SCRIPT_NAME}] ERROR $*" | tee -a "${LOGFILE}"; }

echolog "Logging ${SCRIPT_NAME} actions to ${LOGFILE}"

echolog "Listing Seq environment variables"
env | grep SEQ_

cfg_admin_username=${SEQ_FIRSTRUN_ADMINUSERNAME:-"admin"}
cfg_auth_ingestion=${SEQ_FIRSTRUN_REQUIREAUTHENTICATIONFORHTTPINGESTION:-"true"}
cfg_no_auth=${SEQ_FIRSTRUN_NOAUTHENTICATION:-"false"}

if [[ -n "${SEQ_FIRSTRUN_ADMINPASSWORD}" ]]; then
    echolog "Generating admin password hash from SEQ_FIRSTRUN_ADMINPASSWORD"
    cfg_admin_password_hash=$(echo "${SEQ_FIRSTRUN_ADMINPASSWORD}" | seqsvr config hash)
elif [[ -n "${SEQ_FIRSTRUN_ADMINPASSWORDHASH}" ]]; then
    echolog "Using admin password hash from SEQ_FIRSTRUN_ADMINPASSWORDHASH"
    cfg_admin_password_hash="${SEQ_FIRSTRUN_ADMINPASSWORDHASH}"
else
    echoerr "SEQ_FIRSTRUN_ADMINPASSWORD or SEQ_FIRSTRUN_ADMINPASSWORDHASH is missing"
    exit 1
fi

echolog "Setting firstRun.adminUsername"
seqsvr config set -k firstRun.adminUsername -v "${cfg_admin_username}"

echolog "Setting firstRun.adminPasswordHash"
seqsvr config set -k firstRun.adminPasswordHash -v "${cfg_admin_password_hash}"

echolog "Setting firstRun.noAuthentication"
seqsvr config set -k firstRun.noAuthentication -v "${cfg_no_auth}"

echolog "Setting firstRun.requireAuthenticationForHttpIngestion"
seqsvr config set -k firstRun.requireAuthenticationForHttpIngestion -v "${cfg_auth_ingestion}"

echolog "Listing firstRun.* settings in Seq.json file:"
seqsvr config list | grep firstRun

echolog "Done"
