#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Full ISO-3166-1 country seed generator.

Emits Modules/ReferenceData/.../Seed/Json/Location/countries.json — the flat array
LocationJsonSeedService.SeedAllCountriesAsync reads and upserts into MongoDB
(reference_location_countries). Powers the mobile flag picker's full searchable
list + pinned yacht-flag states (US, MT, MH, IT, GR, GB, FR, NL, PA, KY).

    cd tools/countries-seed
    python3 build.py

Row shape (LocationCountrySeedModel):
    { "countryCode","numericCode","name":{"tr","en"},"defaultCurrencyCode","phoneCode","isActive" }

Data below is compiled from the public ISO-3166-1 standard (alpha-2 + numeric),
ISO-4217 default currency, and E.164 dialing codes; `name.tr` are the standard
Turkish exonyms. `name` always carries `en` (the resolver's fallback) + `tr`.
Stdlib only. Idempotent on re-run; the seed upsert is keyed on countryCode.
"""
import json
import os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(
    HERE, "..", "..",
    "Modules", "ReferenceData", "src",
    "Aizen.Modules.ReferenceData.Repository", "Seed", "Json", "Location", "countries.json"))

# (alpha2, numeric, english, turkish, currency, phone)
COUNTRIES = [
    ("AF", "004", "Afghanistan", "Afganistan", "AFN", "+93"),
    ("AL", "008", "Albania", "Arnavutluk", "ALL", "+355"),
    ("DZ", "012", "Algeria", "Cezayir", "DZD", "+213"),
    ("AD", "020", "Andorra", "Andorra", "EUR", "+376"),
    ("AO", "024", "Angola", "Angola", "AOA", "+244"),
    ("AG", "028", "Antigua and Barbuda", "Antigua ve Barbuda", "XCD", "+1268"),
    ("AR", "032", "Argentina", "Arjantin", "ARS", "+54"),
    ("AM", "051", "Armenia", "Ermenistan", "AMD", "+374"),
    ("AW", "533", "Aruba", "Aruba", "AWG", "+297"),
    ("AU", "036", "Australia", "Avustralya", "AUD", "+61"),
    ("AT", "040", "Austria", "Avusturya", "EUR", "+43"),
    ("AZ", "031", "Azerbaijan", "Azerbaycan", "AZN", "+994"),
    ("BS", "044", "Bahamas", "Bahamalar", "BSD", "+1242"),
    ("BH", "048", "Bahrain", "Bahreyn", "BHD", "+973"),
    ("BD", "050", "Bangladesh", "Bangladeş", "BDT", "+880"),
    ("BB", "052", "Barbados", "Barbados", "BBD", "+1246"),
    ("BY", "112", "Belarus", "Belarus", "BYN", "+375"),
    ("BE", "056", "Belgium", "Belçika", "EUR", "+32"),
    ("BZ", "084", "Belize", "Belize", "BZD", "+501"),
    ("BJ", "204", "Benin", "Benin", "XOF", "+229"),
    ("BM", "060", "Bermuda", "Bermuda", "BMD", "+1441"),
    ("BT", "064", "Bhutan", "Butan", "BTN", "+975"),
    ("BO", "068", "Bolivia", "Bolivya", "BOB", "+591"),
    ("BA", "070", "Bosnia and Herzegovina", "Bosna-Hersek", "BAM", "+387"),
    ("BW", "072", "Botswana", "Botsvana", "BWP", "+267"),
    ("BR", "076", "Brazil", "Brezilya", "BRL", "+55"),
    ("BN", "096", "Brunei", "Brunei", "BND", "+673"),
    ("BG", "100", "Bulgaria", "Bulgaristan", "BGN", "+359"),
    ("BF", "854", "Burkina Faso", "Burkina Faso", "XOF", "+226"),
    ("BI", "108", "Burundi", "Burundi", "BIF", "+257"),
    ("CV", "132", "Cabo Verde", "Cabo Verde", "CVE", "+238"),
    ("KH", "116", "Cambodia", "Kamboçya", "KHR", "+855"),
    ("CM", "120", "Cameroon", "Kamerun", "XAF", "+237"),
    ("CA", "124", "Canada", "Kanada", "CAD", "+1"),
    ("KY", "136", "Cayman Islands", "Cayman Adaları", "KYD", "+1345"),
    ("CF", "140", "Central African Republic", "Orta Afrika Cumhuriyeti", "XAF", "+236"),
    ("TD", "148", "Chad", "Çad", "XAF", "+235"),
    ("CL", "152", "Chile", "Şili", "CLP", "+56"),
    ("CN", "156", "China", "Çin", "CNY", "+86"),
    ("CO", "170", "Colombia", "Kolombiya", "COP", "+57"),
    ("KM", "174", "Comoros", "Komorlar", "KMF", "+269"),
    ("CG", "178", "Congo", "Kongo", "XAF", "+242"),
    ("CD", "180", "Congo (DRC)", "Demokratik Kongo Cumhuriyeti", "CDF", "+243"),
    ("CR", "188", "Costa Rica", "Kosta Rika", "CRC", "+506"),
    ("CI", "384", "Côte d'Ivoire", "Fildişi Sahili", "XOF", "+225"),
    ("HR", "191", "Croatia", "Hırvatistan", "EUR", "+385"),
    ("CU", "192", "Cuba", "Küba", "CUP", "+53"),
    ("CY", "196", "Cyprus", "Kıbrıs", "EUR", "+357"),
    ("CZ", "203", "Czechia", "Çekya", "CZK", "+420"),
    ("DK", "208", "Denmark", "Danimarka", "DKK", "+45"),
    ("DJ", "262", "Djibouti", "Cibuti", "DJF", "+253"),
    ("DM", "212", "Dominica", "Dominika", "XCD", "+1767"),
    ("DO", "214", "Dominican Republic", "Dominik Cumhuriyeti", "DOP", "+1809"),
    ("EC", "218", "Ecuador", "Ekvador", "USD", "+593"),
    ("EG", "818", "Egypt", "Mısır", "EGP", "+20"),
    ("SV", "222", "El Salvador", "El Salvador", "USD", "+503"),
    ("GQ", "226", "Equatorial Guinea", "Ekvator Ginesi", "XAF", "+240"),
    ("ER", "232", "Eritrea", "Eritre", "ERN", "+291"),
    ("EE", "233", "Estonia", "Estonya", "EUR", "+372"),
    ("SZ", "748", "Eswatini", "Esvatini", "SZL", "+268"),
    ("ET", "231", "Ethiopia", "Etiyopya", "ETB", "+251"),
    ("FJ", "242", "Fiji", "Fiji", "FJD", "+679"),
    ("FI", "246", "Finland", "Finlandiya", "EUR", "+358"),
    ("FR", "250", "France", "Fransa", "EUR", "+33"),
    ("PF", "258", "French Polynesia", "Fransız Polinezyası", "XPF", "+689"),
    ("GA", "266", "Gabon", "Gabon", "XAF", "+241"),
    ("GM", "270", "Gambia", "Gambiya", "GMD", "+220"),
    ("GE", "268", "Georgia", "Gürcistan", "GEL", "+995"),
    ("DE", "276", "Germany", "Almanya", "EUR", "+49"),
    ("GH", "288", "Ghana", "Gana", "GHS", "+233"),
    ("GI", "292", "Gibraltar", "Cebelitarık", "GIP", "+350"),
    ("GR", "300", "Greece", "Yunanistan", "EUR", "+30"),
    ("GL", "304", "Greenland", "Grönland", "DKK", "+299"),
    ("GD", "308", "Grenada", "Grenada", "XCD", "+1473"),
    ("GT", "320", "Guatemala", "Guatemala", "GTQ", "+502"),
    ("GN", "324", "Guinea", "Gine", "GNF", "+224"),
    ("GW", "624", "Guinea-Bissau", "Gine-Bissau", "XOF", "+245"),
    ("GY", "328", "Guyana", "Guyana", "GYD", "+592"),
    ("HT", "332", "Haiti", "Haiti", "HTG", "+509"),
    ("HN", "340", "Honduras", "Honduras", "HNL", "+504"),
    ("HK", "344", "Hong Kong", "Hong Kong", "HKD", "+852"),
    ("HU", "348", "Hungary", "Macaristan", "HUF", "+36"),
    ("IS", "352", "Iceland", "İzlanda", "ISK", "+354"),
    ("IN", "356", "India", "Hindistan", "INR", "+91"),
    ("ID", "360", "Indonesia", "Endonezya", "IDR", "+62"),
    ("IR", "364", "Iran", "İran", "IRR", "+98"),
    ("IQ", "368", "Iraq", "Irak", "IQD", "+964"),
    ("IE", "372", "Ireland", "İrlanda", "EUR", "+353"),
    ("IL", "376", "Israel", "İsrail", "ILS", "+972"),
    ("IT", "380", "Italy", "İtalya", "EUR", "+39"),
    ("JM", "388", "Jamaica", "Jamaika", "JMD", "+1876"),
    ("JP", "392", "Japan", "Japonya", "JPY", "+81"),
    ("JO", "400", "Jordan", "Ürdün", "JOD", "+962"),
    ("KZ", "398", "Kazakhstan", "Kazakistan", "KZT", "+7"),
    ("KE", "404", "Kenya", "Kenya", "KES", "+254"),
    ("KI", "296", "Kiribati", "Kiribati", "AUD", "+686"),
    ("KW", "414", "Kuwait", "Kuveyt", "KWD", "+965"),
    ("KG", "417", "Kyrgyzstan", "Kırgizistan", "KGS", "+996"),
    ("LA", "418", "Laos", "Laos", "LAK", "+856"),
    ("LV", "428", "Latvia", "Letonya", "EUR", "+371"),
    ("LB", "422", "Lebanon", "Lübnan", "LBP", "+961"),
    ("LS", "426", "Lesotho", "Lesotho", "LSL", "+266"),
    ("LR", "430", "Liberia", "Liberya", "LRD", "+231"),
    ("LY", "434", "Libya", "Libya", "LYD", "+218"),
    ("LI", "438", "Liechtenstein", "Lihtenştayn", "CHF", "+423"),
    ("LT", "440", "Lithuania", "Litvanya", "EUR", "+370"),
    ("LU", "442", "Luxembourg", "Lüksemburg", "EUR", "+352"),
    ("MO", "446", "Macao", "Makao", "MOP", "+853"),
    ("MG", "450", "Madagascar", "Madagaskar", "MGA", "+261"),
    ("MW", "454", "Malawi", "Malavi", "MWK", "+265"),
    ("MY", "458", "Malaysia", "Malezya", "MYR", "+60"),
    ("MV", "462", "Maldives", "Maldivler", "MVR", "+960"),
    ("ML", "466", "Mali", "Mali", "XOF", "+223"),
    ("MT", "470", "Malta", "Malta", "EUR", "+356"),
    ("MH", "584", "Marshall Islands", "Marshall Adaları", "USD", "+692"),
    ("MR", "478", "Mauritania", "Moritanya", "MRU", "+222"),
    ("MU", "480", "Mauritius", "Mauritius", "MUR", "+230"),
    ("MX", "484", "Mexico", "Meksika", "MXN", "+52"),
    ("FM", "583", "Micronesia", "Mikronezya", "USD", "+691"),
    ("MD", "498", "Moldova", "Moldova", "MDL", "+373"),
    ("MC", "492", "Monaco", "Monako", "EUR", "+377"),
    ("MN", "496", "Mongolia", "Moğolistan", "MNT", "+976"),
    ("ME", "499", "Montenegro", "Karadağ", "EUR", "+382"),
    ("MA", "504", "Morocco", "Fas", "MAD", "+212"),
    ("MZ", "508", "Mozambique", "Mozambik", "MZN", "+258"),
    ("MM", "104", "Myanmar", "Myanmar", "MMK", "+95"),
    ("NA", "516", "Namibia", "Namibya", "NAD", "+264"),
    ("NR", "520", "Nauru", "Nauru", "AUD", "+674"),
    ("NP", "524", "Nepal", "Nepal", "NPR", "+977"),
    ("NL", "528", "Netherlands", "Hollanda", "EUR", "+31"),
    ("NC", "540", "New Caledonia", "Yeni Kaledonya", "XPF", "+687"),
    ("NZ", "554", "New Zealand", "Yeni Zelanda", "NZD", "+64"),
    ("NI", "558", "Nicaragua", "Nikaragua", "NIO", "+505"),
    ("NE", "562", "Niger", "Nijer", "XOF", "+227"),
    ("NG", "566", "Nigeria", "Nijerya", "NGN", "+234"),
    ("MK", "807", "North Macedonia", "Kuzey Makedonya", "MKD", "+389"),
    ("NO", "578", "Norway", "Norveç", "NOK", "+47"),
    ("OM", "512", "Oman", "Umman", "OMR", "+968"),
    ("PK", "586", "Pakistan", "Pakistan", "PKR", "+92"),
    ("PW", "585", "Palau", "Palau", "USD", "+680"),
    ("PS", "275", "Palestine", "Filistin", "ILS", "+970"),
    ("PA", "591", "Panama", "Panama", "PAB", "+507"),
    ("PG", "598", "Papua New Guinea", "Papua Yeni Gine", "PGK", "+675"),
    ("PY", "600", "Paraguay", "Paraguay", "PYG", "+595"),
    ("PE", "604", "Peru", "Peru", "PEN", "+51"),
    ("PH", "608", "Philippines", "Filipinler", "PHP", "+63"),
    ("PL", "616", "Poland", "Polonya", "PLN", "+48"),
    ("PT", "620", "Portugal", "Portekiz", "EUR", "+351"),
    ("QA", "634", "Qatar", "Katar", "QAR", "+974"),
    ("RO", "642", "Romania", "Romanya", "RON", "+40"),
    ("RU", "643", "Russia", "Rusya", "RUB", "+7"),
    ("RW", "646", "Rwanda", "Ruanda", "RWF", "+250"),
    ("KN", "659", "Saint Kitts and Nevis", "Saint Kitts ve Nevis", "XCD", "+1869"),
    ("LC", "662", "Saint Lucia", "Saint Lucia", "XCD", "+1758"),
    ("VC", "670", "Saint Vincent and the Grenadines", "Saint Vincent ve Grenadinler", "XCD", "+1784"),
    ("WS", "882", "Samoa", "Samoa", "WST", "+685"),
    ("SM", "674", "San Marino", "San Marino", "EUR", "+378"),
    ("ST", "678", "Sao Tome and Principe", "Sao Tome ve Principe", "STN", "+239"),
    ("SA", "682", "Saudi Arabia", "Suudi Arabistan", "SAR", "+966"),
    ("SN", "686", "Senegal", "Senegal", "XOF", "+221"),
    ("RS", "688", "Serbia", "Sırbistan", "RSD", "+381"),
    ("SC", "690", "Seychelles", "Seyşeller", "SCR", "+248"),
    ("SL", "694", "Sierra Leone", "Sierra Leone", "SLE", "+232"),
    ("SG", "702", "Singapore", "Singapur", "SGD", "+65"),
    ("SK", "703", "Slovakia", "Slovakya", "EUR", "+421"),
    ("SI", "705", "Slovenia", "Slovenya", "EUR", "+386"),
    ("SB", "090", "Solomon Islands", "Solomon Adaları", "SBD", "+677"),
    ("SO", "706", "Somalia", "Somali", "SOS", "+252"),
    ("ZA", "710", "South Africa", "Güney Afrika", "ZAR", "+27"),
    ("KR", "410", "South Korea", "Güney Kore", "KRW", "+82"),
    ("SS", "728", "South Sudan", "Güney Sudan", "SSP", "+211"),
    ("ES", "724", "Spain", "İspanya", "EUR", "+34"),
    ("LK", "144", "Sri Lanka", "Sri Lanka", "LKR", "+94"),
    ("SD", "729", "Sudan", "Sudan", "SDG", "+249"),
    ("SR", "740", "Suriname", "Surinam", "SRD", "+597"),
    ("SE", "752", "Sweden", "İsveç", "SEK", "+46"),
    ("CH", "756", "Switzerland", "İsviçre", "CHF", "+41"),
    ("SY", "760", "Syria", "Suriye", "SYP", "+963"),
    ("TW", "158", "Taiwan", "Tayvan", "TWD", "+886"),
    ("TJ", "762", "Tajikistan", "Tacikistan", "TJS", "+992"),
    ("TZ", "834", "Tanzania", "Tanzanya", "TZS", "+255"),
    ("TH", "764", "Thailand", "Tayland", "THB", "+66"),
    ("TL", "626", "Timor-Leste", "Doğu Timor", "USD", "+670"),
    ("TG", "768", "Togo", "Togo", "XOF", "+228"),
    ("TO", "776", "Tonga", "Tonga", "TOP", "+676"),
    ("TT", "780", "Trinidad and Tobago", "Trinidad ve Tobago", "TTD", "+1868"),
    ("TN", "788", "Tunisia", "Tunus", "TND", "+216"),
    ("TR", "792", "Türkiye", "Türkiye", "TRY", "+90"),
    ("TM", "795", "Turkmenistan", "Türkmenistan", "TMT", "+993"),
    ("TC", "796", "Turks and Caicos Islands", "Turks ve Caicos Adaları", "USD", "+1649"),
    ("TV", "798", "Tuvalu", "Tuvalu", "AUD", "+688"),
    ("UG", "800", "Uganda", "Uganda", "UGX", "+256"),
    ("UA", "804", "Ukraine", "Ukrayna", "UAH", "+380"),
    ("AE", "784", "United Arab Emirates", "Birleşik Arap Emirlikleri", "AED", "+971"),
    ("GB", "826", "United Kingdom", "Birleşik Krallık", "GBP", "+44"),
    ("US", "840", "United States", "Amerika Birleşik Devletleri", "USD", "+1"),
    ("UY", "858", "Uruguay", "Uruguay", "UYU", "+598"),
    ("UZ", "860", "Uzbekistan", "Özbekistan", "UZS", "+998"),
    ("VU", "548", "Vanuatu", "Vanuatu", "VUV", "+678"),
    ("VA", "336", "Vatican City", "Vatikan", "EUR", "+379"),
    ("VE", "862", "Venezuela", "Venezuela", "VES", "+58"),
    ("VN", "704", "Vietnam", "Vietnam", "VND", "+84"),
    ("VG", "092", "Virgin Islands (British)", "Britanya Virjin Adaları", "USD", "+1284"),
    ("YE", "887", "Yemen", "Yemen", "YER", "+967"),
    ("ZM", "894", "Zambia", "Zambiya", "ZMW", "+260"),
    ("ZW", "716", "Zimbabwe", "Zimbabve", "USD", "+263"),
]


def main():
    seen = set()
    rows = []
    for code, numeric, en, tr, ccy, phone in COUNTRIES:
        c = code.strip().upper()
        if c in seen:
            raise SystemExit(f"duplicate country code {c}")
        seen.add(c)
        rows.append({
            "countryCode": c,
            "numericCode": numeric,
            "name": {"tr": tr, "en": en},
            "defaultCurrencyCode": ccy,
            "phoneCode": phone,
            "isActive": True,
        })
    rows.sort(key=lambda r: r["name"]["en"])

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as f:
        json.dump(rows, f, ensure_ascii=False, indent=2)
        f.write("\n")

    pinned = ["US", "MT", "MH", "IT", "GR", "GB", "FR", "NL", "PA", "KY"]
    missing = [p for p in pinned if p not in seen]
    print(f"wrote {len(rows)} countries -> {OUT}")
    print(f"pinned yacht-flag states present: {'ALL' if not missing else 'MISSING ' + ','.join(missing)}")


if __name__ == "__main__":
    main()
