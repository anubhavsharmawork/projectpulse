namespace Domain.Enums
{
    public enum AssetType
    {
        // ── General ──
        Equipment = 1,
        Vehicle = 2,
        ITHardware = 3,
        Tool = 4,
        Furniture = 5,
        Material = 6,

        // ── IT / Technology ──
        Server = 10,
        Laptop = 11,
        Desktop = 12,
        Monitor = 13,
        NetworkSwitch = 14,
        Router = 15,
        Firewall = 16,
        MobileDevice = 17,
        Printer = 18,
        SoftwareLicense = 19,
        CloudSubscription = 20,

        // ── Healthcare ──
        MedicalDevice = 30,
        ImagingEquipment = 31,
        LabInstrument = 32,
        PatientMonitor = 33,
        Defibrillator = 34,
        InfusionPump = 35,
        SurgicalInstrument = 36,
        HospitalBed = 37,
        Wheelchair = 38,
        Ambulance = 39,
        PharmaceuticalStorage = 40,

        // ── Public Safety ──
        PatrolVehicle = 50,
        BodyCamera = 51,
        Radio = 52,
        Firearm = 53,
        ProtectiveGear = 54,
        TacticalEquipment = 55,
        ForensicKit = 56,
        EmergencyVehicle = 57,
        Drone = 58,
        SurveillanceSystem = 59,

        // ── Construction ──
        Excavator = 70,
        Crane = 71,
        Scaffolding = 72,
        Bulldozer = 73,
        ConcreteEquipment = 74,
        SurveyingInstrument = 75,
        SafetyEquipment = 76,
        PowerTool = 77,
        HeavyTruck = 78,
        Generator = 79,

        // ── Infrastructure / Utility (e.g. energy, water, telecoms) ──
        Transformer = 90,
        PowerLine = 91,
        SmartMeter = 92,
        Substation = 93,
        Pipeline = 94,
        PumpingStation = 95,
        WaterMain = 96,
        TelecomTower = 97,
        TrafficSignal = 98,
        Bridge = 100,
        SolarPanel = 101,
        WindTurbine = 102,

        // ── Economic Development ──
        OfficeEquipment = 110,
        CommunityFacility = 111,
        GrantFundedAsset = 112,
        ConferenceSystem = 113,
        PublicVehicle = 114,

        // ── Technology / R&D ──
        DevWorkstation = 120,
        TestDevice = 121,
        ServerRack = 122,
        PrototypingEquipment = 123,
        ARVRHeadset = 124,
        RoboticSystem = 125,
        ThreeDPrinter = 126,

        Other = 999
    }
}
