# 06 — Create Inktavia Marine OS Lookup Tree and Items Seeds

Create JSON files for LookupGroup tree and LookupItems.

## Files

```text
Aizen.Modules.ReferenceData.Repository/Seed/Json/Lookup/lookup-groups.json
Aizen.Modules.ReferenceData.Repository/Seed/Json/Lookup/lookup-items.json
Aizen.Modules.ReferenceData.Repository/Seed/Json/Lookup/lookup-tree-marine.json
```

## LookupGroup tree

Create groups with parent-child relationship using `ParentCode`.

Required tree:

```text
MARINE
  VESSEL
    VESSEL_TYPE
    VESSEL_USAGE_TYPE
    HULL_MATERIAL
    ENGINE_TYPE
    FUEL_TYPE
    VESSEL_STATUS
    VESSEL_DOCUMENT_TYPE
  SERVICE_PROVIDER
    SERVICE_PROVIDER_CATEGORY
    MAINTENANCE_SERVICE_TYPE
    CLEANING_SERVICE_TYPE
    FOOD_BEVERAGE_SERVICE_TYPE
    EMERGENCY_SERVICE_TYPE
    PROVIDER_STATUS
  CARGODRY
    CARGODRY_PRODUCT_TYPE
    CARGODRY_PACKAGE_TYPE
    CARGODRY_RISK_LEVEL
    CARGODRY_USAGE_AREA
    CARGODRY_KIT_STATUS
  SENSOR
    SENSOR_TYPE
    SENSOR_STATUS
    SENSOR_MEASUREMENT_TYPE
    SENSOR_CONNECTIVITY_TYPE
SYSTEM
  DOCUMENT_TYPE
  FILE_TYPE
  NOTIFICATION_TYPE
  LANGUAGE
  TIME_ZONE
  COUNTRY_PHONE_CODE
PAYMENT
  PAYMENT_METHOD_TYPE
  PAYMENT_STATUS
  CURRENCY_USAGE_TYPE
  COMMISSION_TYPE
```

## LookupItems

Add items for each group.

### VESSEL_TYPE

```text
MOTOR_YACHT
SAILING_BOAT
CATAMARAN
FISHING_BOAT
SERVICE_BOAT
COMMERCIAL_VESSEL
RIB
JET_SKI
```

### SERVICE_PROVIDER_CATEGORY

```text
MOTOR_MAINTENANCE
BOAT_CLEANING
FOOD_AND_BEVERAGE
FUEL_SUPPORT
ELECTRICAL_SERVICE
INTERIOR_CLEANING
EMERGENCY_REPAIR
MARINA_SUPPORT
PAINTING_POLISHING
WINTERIZATION
```

### CARGODRY_PRODUCT_TYPE

```text
CARGODRY_BASIC
CARGODRY_SENSE
CARGODRY_PRO
```

### CARGODRY_USAGE_AREA

```text
CABIN
ENGINE_ROOM
BILGE
DECK_STORAGE
WARDROBE
GALLEY
BATHROOM
TECHNICAL_ROOM
```

### CARGODRY_RISK_LEVEL

```text
LOW
MEDIUM
HIGH
CRITICAL
```

### SENSOR_TYPE

```text
HUMIDITY
TEMPERATURE
WATER_LEAK
BATTERY
DOOR_OPEN
MULTI_SENSOR
```

### PAYMENT_STATUS

```text
PENDING
PAID
FAILED
CANCELED
REFUNDED
PARTIALLY_REFUNDED
```

## Seed behavior

- Create parent groups before child groups.
- Use `Code` as unique key.
- Use `ParentCode` to calculate `ParentLookupGroupId`, `Level`, `HierarchyPath`.
- Insert missing groups only.
- Update names/descriptions/sort orders if seed policy allows updates.
- Insert missing items only.
- Do not duplicate items.

## Output

Report created lookup groups and items.
