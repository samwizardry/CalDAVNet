namespace CalDAVNet;

/// <summary>
/// Effective privileges of the current user on a calendar collection (CalDAV).
/// </summary>
[Flags]
public enum CurrentUserPrivilegeSet : int
{
    /// <summary>
    /// No privileges.
    /// </summary>
    None = 0,

    /// <summary>
    /// &lt;D:read/&gt; (DAV:)
    /// View calendar (list of events, depends on event visibility).
    /// Almost always present, even for guest / read-only access.
    /// </summary>
    Read = 1 << 0,

    /// <summary>
    /// &lt;D:read-free-busy/&gt; (urn:ietf:params:xml:ns:caldav)
    /// Access to free/busy information only (busy/free status), without event details.
    /// Rarely returned separately; usually appears together with Read.
    /// </summary>
    ReadFreeBusy = 1 << 1,

    /// <summary>
    /// &lt;D:write/&gt; (DAV:)
    /// Full editing: create, modify, delete events + modify calendar properties.
    /// Typically granted to the calendar owner.
    /// </summary>
    Write = 1 << 2,

    /// <summary>
    /// &lt;D:write-content/&gt; (DAV:)
    /// Modify calendar content (events), but not collection properties
    /// (e.g. display name, color, description).
    /// Often appears together with Write.
    /// </summary>
    WriteContent = 1 << 3,

    /// <summary>
    /// &lt;D:write-properties/&gt; (DAV:)
    /// Modify calendar metadata (displayname, color, description, etc.).
    /// Usually granted only to the owner.
    /// </summary>
    WriteProperties = 1 << 4,

    /// <summary>
    /// &lt;D:bind/&gt; (DAV:)
    /// Add resources (events) to the collection.
    /// Conceptually part of Write; typically present for the owner.
    /// </summary>
    Bind = 1 << 5,

    /// <summary>
    /// &lt;D:unbind/&gt; (DAV:)
    /// Remove resources (events) from the collection.
    /// Conceptually part of Write; typically present for the owner.
    /// </summary>
    Unbind = 1 << 6,

    /// <summary>
    /// &lt;D:read-acl/&gt; (DAV:)
    /// Read ACL (access control list).
    /// Rarely available; in Yandex CalDAV usually not fully supported.
    /// </summary>
    ReadAcl = 1 << 7,

    /// <summary>
    /// &lt;D:write-acl/&gt; (DAV:)
    /// Modify access control rules.
    /// Not supported in Yandex CalDAV.
    /// </summary>
    WriteAcl = 1 << 8,

    /// <summary>
    /// &lt;D:all/&gt; (DAV:)
    /// Aggregate of all possible privileges.
    /// Very rarely returned explicitly.
    /// </summary>
    All = Read | ReadFreeBusy | Write | WriteContent | WriteProperties | Bind | Unbind | ReadAcl | WriteAcl
}
