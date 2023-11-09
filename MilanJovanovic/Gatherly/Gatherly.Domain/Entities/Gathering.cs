namespace Gatherly.Domain.Entities;

public class Gathering
{
    private readonly List<Invitation> _invitations = new();
    private readonly List<Attendee> _attendees = new();

    private Gathering(
        Guid id,
        Member creator,
        GatheringType type,
        DateTime scheduledAtUtc,
        string name,
        string? location)
    {
        Id = id;
        Creator = creator;
        Type = type;
        ScheduledAtUtc = scheduledAtUtc;
        Name = name;
        Location = location;
    }

    public Guid Id { get; private set; }
    public Member Creator { get; private set; }
    public GatheringType Type { get; private set; }
    public string Name { get; private set; }
    public DateTime ScheduledAtUtc { get; private set; }
    public string? Location { get; private set; }
    public int? MaximumNumberOfAttendees { get; private set; }
    public DateTime? InvitationsExpiredAtUtc { get; private set; }
    public int NumberOfAttendees { get; private set; }
    public IReadOnlyCollection<Attendee> Attendees => _attendees;
    public IReadOnlyCollection<Invitation> Invitations => _invitations;

    public static Gathering Create(
        Guid id,
        Member creator,
        GatheringType type,
        DateTime scheduledAtUtc,
        string name,
        string? location,
        int? maximumNumberOfAttendees,
        int? invitationsValidatBeforeInHours)
    {
        var gathering =
            new Gathering(id, creator, type, scheduledAtUtc, name, location);

        switch (gathering.Type)
        {
            case GatheringType.WithFixedNumberOfAttendees:
                if (maximumNumberOfAttendees is null)
                {
                    throw new Exception($"{nameof(maximumNumberOfAttendees)} can't be null.");
                }

                gathering.MaximumNumberOfAttendees = maximumNumberOfAttendees;
                break;
            case GatheringType.WithExpirationForInvitations:
                if (invitationsValidatBeforeInHours is null)
                {
                    throw new Exception($"{nameof(invitationsValidatBeforeInHours)} can't be null.");
                }

                gathering.InvitationsExpiredAtUtc =
                    gathering.ScheduledAtUtc.AddHours(-invitationsValidatBeforeInHours.Value);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(GatheringType));
        }

        return gathering;
    }

    public Invitation SendInvitation(Member member)
    {
        if (Creator.Id == member.Id)
        {
            throw new Exception("Can't send invitation to the gathering creator.");
        }

        if (ScheduledAtUtc < DateTime.UtcNow)
        {
            throw new Exception("Can't send invitation for gathering in the past.");
        }

        var invitation = new Invitation(Guid.NewGuid(), member, this);

        _invitations.Add(invitation);

        return invitation;
    }

    public Attendee? AcceptInvitation(Invitation invitation)
    {
        var expired = (Type == GatheringType.WithFixedNumberOfAttendees &&
               NumberOfAttendees == MaximumNumberOfAttendees) ||
              (Type == GatheringType.WithExpirationForInvitations &&
               InvitationsExpiredAtUtc < DateTime.UtcNow);

        if (expired)
        {
            invitation.Expire();

            return null;
        }

        var attendee = invitation.Accept();

        _attendees.Add(attendee);
        NumberOfAttendees++;

        return attendee;
    }
}
