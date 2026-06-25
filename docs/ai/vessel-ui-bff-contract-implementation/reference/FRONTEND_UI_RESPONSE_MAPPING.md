# Frontend UI Response Mapping

## AF — Vessel Management List

Needs:

- Vessel identity and metadata
- Owner display data
- Primary thumbnail
- Dimensions
- Latest location
- status fields
- pagination

## AG — Vessel Detail Overview

Needs:

- Full vessel base data
- Specification fields
- Engine data
- Primary hero image
- CargoDry kits
- service history
- document summaries

## AH — Vessel Documents & Media

Needs:

- Full document list with versions
- Approval state
- version metadata
- media list with image/video type, thumbnails, uploaded user, primary marker and sort order

## Derived fields

BFF computes:

- document daysUntilExpiry
- documentStatus
- CargoDry daysUntilExpiry
- CargoDry kit status
- version isCurrent

Do not require frontend to derive these values.
