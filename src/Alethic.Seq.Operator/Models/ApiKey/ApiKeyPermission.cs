using System;

namespace Alethic.Seq.Operator.Models.ApiKey
{

    public enum ApiKeyPermission
    {

        /// <summary>
        /// A sentinel value to detect uninitialized permissions.
        /// </summary>
        Undefined,

        /// <summary>
        /// Access to publicly-visible assets - the API root/metadata, HTML, JavaScript, CSS, information necessary
        /// to initiate the login process, and so-on.
        /// </summary>
        Public,

        /// <summary>
        /// Add events to the event store.
        /// </summary>
        Ingest,

        /// <summary>
        /// Query events, dashboards, signals, app instances.
        /// </summary>
        Read,

        /// <summary>
        /// Write-access to signals, alerts, preferences etc.
        /// </summary>
        Write,

        /// <summary>
        /// Access to administrative features of Seq, management of other users, app installation, backups.
        /// </summary>
        [Obsolete("The `Setup` permission has been replaced by `Project` and `System`.")]
        Setup,

        /// <summary>
        /// Access to settings that control data ingestion, storage, dashboarding and alerting.
        /// </summary>
        Project,

        /// <summary>
        /// Access to settings and features that interact with, or provide access to, the underlying host server,
        /// such as app (plug-in) installation, backup settings, cluster configuration, diagnostics, and features
        /// relying on outbound network access like package feeds and update checks. This permission is required for
        /// configuration of the authentication provider and related settings.
        /// </summary>
        System,

        /// <summary>
        /// Create, edit, and delete user accounts, reset local user passwords.
        /// </summary>
        Organization,

    }

}
