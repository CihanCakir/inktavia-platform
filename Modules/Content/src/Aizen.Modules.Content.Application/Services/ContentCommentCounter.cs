using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Modules.Content.Application.Services;

/// <summary>
/// CommentCount tracks APPROVED comments only (what the public sees). The delta for a status transition
/// is simply the change in "approved-ness":
///   delta = (newStatus == Approved ? 1 : 0) - (oldStatus == Approved ? 1 : 0)
///
/// Matrix (old → new):
///   Pending  → Approved  = +1     Approved → Hidden   = -1
///   Rejected → Approved  = +1     Approved → Rejected = -1
///   Hidden   → Approved  = +1     Pending  → Hidden    =  0
///   any      → same       =  0     Rejected → Hidden    =  0
/// Deleting a comment that was Approved = -1, else 0.
/// </summary>
public static class ContentCommentCounter
{
    public static int TransitionDelta(ContentCommentStatus oldStatus, ContentCommentStatus newStatus)
        => (newStatus == ContentCommentStatus.Approved ? 1 : 0)
         - (oldStatus == ContentCommentStatus.Approved ? 1 : 0);

    public static int DeleteDelta(ContentCommentStatus currentStatus)
        => currentStatus == ContentCommentStatus.Approved ? -1 : 0;
}
