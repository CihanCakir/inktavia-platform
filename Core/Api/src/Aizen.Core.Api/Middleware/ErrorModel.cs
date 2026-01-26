namespace Aizen.Core.Api.Middleware;

public enum AizenErrorCode
{
    // 📌 Ortak / Generic
    UnknownError = 1,
    ValidationError = 2,
    NotFound = 3,
    Unauthorized = 4,

    AlreadyDeleted = 5,
    ApplicationNotFound = 100,
    AgeLimitExceeded = 101,
    AdultContentRestricted = 102,
    CountryNotAllowed = 103,
    PhoneAlreadyRegistered = 104,
    PhoneUsedInOtherApplication = 105,
    CurrentDeviceHasBeenLockup = 106,
    DeviceNotAllowed = 107,
    DeviceNotFound = 108,
    DeviceNotSupported = 109,
    DeviceNotApproved = 110,
    AgreementNotFound = 111,
    AgreementNotApproved = 112,
    AgreementNotFoundForUser = 113,
    AgreementNotApprovedForUser = 114,
    AgreementNotApprovedForUserByType = 115,
    AgreementNotApprovedForUserByTypeAndVersion = 116,
    AgreementNotApprovedForUserByTypeAndVersionAndId = 117,
    AgreementAlreadyApproved = 118,
    DeviceAlreadyBlocked = 119,
    DeviceBlockedDueToExcessLoginAttempt = 120,
    DeviceBlockedDueToExcessLoginAttemptAndUserCount = 121,
    UsernameOrPinWrong = 122,
    UserNotFound = 123,
    UserNotActive = 124,
    UserNotConfirmed = 125,
    UserNotAuthorized = 126,
    UserNotFoundForPhone = 127,
    UserNotFoundForEmail = 128,
    UserNotFoundForUsername = 129,
    UserNotFoundForDevice = 130,
    UserNameOrPasswordWrong = 131,
    UserAlreadyExists = 132,
    UserAlreadyExistsForPhone = 133,
    UserAlreadyExistsForEmail = 134,
    UserAlreadyExistsForUsername = 135,
    LoginFailedForPasswordBlockedUser = 136,
    UserAlreadyAcceptedTerms = 137,
    UserHasNoActiveProfileInThisPanel = 138,
    UserHasNoActiveProfileInThisPanelForRole = 139,
    UserHasNoActiveProfileInThisPanelForRoleAndContext = 140,
    PhoneNotConfirmed = 141,
    EmailNotConfirmed = 142,
    OtpWrong = 143,
    OtpExpired = 144,
    OtpNotFound = 145,
    OtpAlreadyUsed = 146,
    OtpNotAllowed = 147,
    OtpNotAllowedForUser = 148,
    OtpNotAllowedForUserAndDevice = 149,
    PassiveUser = 150,
    OtpCodeIsNotValid = 151,
    OtpCodeIsNotValidForUser = 152,
    OtpCodeIsNotValidForUserAndDevice = 153,
    OtpCodeIsNotValidForUserAndDeviceAndContext = 154,
    otpCannotBeNull = 155,
    otpMustBe6Digits = 156,

    // Telefon numarası ile ilgili kodlar
    phoneNumberCannotBeNull = 157,
    phoneNumberMustBeTenDigits = 158,

    // ValidationGuid ile ilgili kodlar
    validationGuidCannotBeNull = 159,

    AnErrorOccurred = 160,
    cityIdMustBeBetween1to81 = 161,
    districtIdMustBeGreaterThan0 = 162,
    neighborhoodIdMustBeGreaterThan0 = 163,
    mallIdMustBeGreaterThan0 = 164,
    storeIdMustBeGreaterThan0 = 165,
    storeNotFoundInMall = 166,


    // Genel/Kayıt
    RequiredAgreementsMissing = 1101,
    UserCreationFailed = 1102,
    PasswordSetFailed = 1103,

    EmailConflictWithExistingUser = 1104,
    PhoneConflictWithExistingUser = 1105,

    UserAlreadyHasProfileOfThisType = 1106,

    RoleNotFound = 1107,
    RoleAssignFailed = 1108,

    ProfileCreateFailed = 1109,

    ProfileAlreadyApproved = 1113,
    ProfileAlreadyRejected = 1114,
    RejectReasonRequired = 1115,
    CannotRejectActiveProfile = 1116,
    ProfileStatusInvalidForAction = 1117,



    // 1200–1299: OAuth / External Login
    OAuthInvalidState = 1200,
    OAuthTokenExchangeFailed = 1201,
    OAuthIdTokenInvalid = 1202,
    OAuthNonceMismatch = 1203,

    ExternalUserNotFound = 1210,
    ParticipantRoleNotFound = 1211,
    ExternalLoginConflict = 1212, // (opsiyonel) aynı provider+sub çakışması vs.
    // 📌 Venue - Temel
    VenueNameRequired = 2000,
    VenueDescriptionRequired = 2001,
    VenueAddressRequired = 2002,
    VenueCapacityInvalid = 2003,
    VenueHourlyRateInvalid = 2004,
    VenueInvalidStatusTransition = 2005,

    // 📌 Venue - Location
    VenueLocationInvalid = 2100,

    // 📌 Venue - Image
    VenueImageUrlRequired = 2200,
    VenueImageNotFound = 2201,

    // 📌 Venue - Availability
    VenueAvailabilityInvalidRange = 2300,
    VenueAvailabilityOverlaps = 2301,
    VenueAvailabilityNotFound = 2302,

    // 📌 Venue - Feature
    VenueFeatureAlreadyExists = 2400,
    VenueFeatureNotFound = 2401,

    // 📌 Venue - Tag
    VenueTagAlreadyExists = 2500,
    VenueTagNotFound = 2501,

    // 📌 Venue - Rating
    VenueRatingInvalidScore = 2600,
    VenueRatingNotFound = 2601,

    // 📌 Venue - Offer
    VenueOfferNotFound = 2700,
    VenueOfferInvalidState = 2701,
    VenueOfferAlreadyAccepted = 2702,
    ActivityNotApprovedForVenueListing = 2706,
    VenueOfferNegotiationLimitReached = 2707,
    VenueOfferExpired = 2708,
    VenueOfferCounterTurnViolation = 2709,
    VenueOfferPriceInvalid = 2710,
    VenueOfferScheduleNotFound = 2711,
    VenueNotAvailable = 2712,
    VenueOfferAlreadyFinalized = 2713,
    // Geo
    LatitudeOutOfRange = 2800,
    LongitudeOutOfRange = 2801,

    VenueAvailabilityInvalidSeason = 2303,

    VenueFeatureExtraPriceInvalid = 2402,

    VenueAvailabilityInvalidTimeRange = 2300,
    DuplicateRating = 2400,
    AlreadyVerified = 2500,
    NotEligibleForVerification = 2501,

    // 📌 Activity - Temel
    ActivityInvalidDateRange = 3000,
    ActivityInvalidCapacity = 3001,
    ActivityImmutableState = 3002,
    ActivityPublishNotAllowed = 3003,
    ActivityPublishPastEvent = 3004,
    ActivityOnlineUrlRequired = 3005,

    // 📌 Activity - Image
    ActivityImageUrlRequired = 3100,

    // 📌 Activity - Schedule
    ActivityScheduleInvalidRange = 3200,
    ActivityScheduleNotFound = 3201,

    // 📌 Activity - Tag
    ActivityTagAlreadyExists = 3300,
    ActivityTagNotFound = 3301,
    // 📌 Activity - Invitation
    ActivityInvitationInvalidEmail = 3400,
    ActivityInvitationInvalidCode = 3401,
    ActivityInvitationExpired = 3402,
    ActivityInvitationAlreadyUsed = 3403,
    ActivityInvitationRevoked = 3404,
    ActivityInvitationNotAllowedState = 3405,
    ActivityInvitationResendThrottle = 3406,

    // 📌 Activity - Participant
    ActivityParticipantAlreadyExists = 3500,
    ActivityParticipantCapacityFull = 3501,
    ActivityParticipantWaitlistNotAllowed = 3502, // (gerekirse)
    ActivityParticipantJoinWindowClosed = 3503,
    ActivityParticipantImmutableState = 3504, // (gerekirse)
    ActivityParticipantNotFound = 3505,
    ActivityParticipantNotInWaitlist = 3506,
    ActivityParticipantAlreadyCancelled = 3507,
    ActivityParticipantAlreadyConfirmed = 3508,
    ActivityParticipantPerUserLimitExceeded = 3509,
    // 📌 Activity - QR
    ActivityQrInvalid = 3600,
    ActivityQrExpired = 3601,
    ActivityQrAlreadyScanned = 3602,
    ActivityQrRevoked = 3603,
    ActivityQrThrottle = 3604,



    TokenNotFound = 40101,
    RefreshTokenTimeOut = 40102,
    UnknownApplicationContext = 40091,
    AccessTokenRequiredForThisPanel = 40111,
    DeviceIdRequired = 40031,
    ProfileSuspended = 3710,
    VenueInactive = 3711,
    ActivityTooSoon = 3712,
    ActivityTooLong = 3713,
    CurrencyNotAllowed = 3714,
    InvalidPricePrecision = 3715,
    // Removed duplicate ActivityOnlineUrlRequired
    InvalidOnlineUrl = 3717,
    TimeOverlap = 3718,
    VenueBooked = 3719,
    RateLimited = 3720,
    PayoutNotConfigured = 3721,
    ModerationFailed = 3722,
    PayloadTooLarge = 3723,
    ProfileNotFound = 3724,
    VenueNotFound = 3725,
    InvalidState= 3726
}
