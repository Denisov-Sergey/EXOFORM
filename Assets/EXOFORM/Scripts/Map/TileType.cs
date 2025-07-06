namespace Exoform.Scripts.Map
{
    public enum TileType
    {
        // Базовые типы поверхности
        Grass,                    // ✅ Трава (оставляем)
        PathwayStraight,          // 🔄 Пути между зонами (было RoadStraight)
        
        // === EXOFORM ЗОНЫ ===
        StandardZone,             // 🟢 Стандартная зона с ресурсами
        TechnicalZone,            // 🔧 Техническая зона (восстановление техники)
        ArtifactZone,             // 🧬 Зона артефактов (опасная)
        CorruptedTrap,            // ⚠️ Заражённая зона-ловушка
        InfestationZone,         // 🦠 Зона инфестации (порча разрастается)
        BossZone,                // 👹 Зона босса
        
        // === СТРУКТУРЫ  ===
        Structure,                // 🏠 Базовая структура
        LargeStructure,          // 🏢 Большая структура
        ResearchFacility,        // 🏬 Исследовательский объект
        ProcessingPlant,         // 🏭 Перерабатывающий завод 
        BioDome,                 // 🏞️ Био-купол
        CommandCenter,           // 🏛️ Командный центр 
        ContainmentUnit,         // 🔒 Блок сдерживания 

        // === РАСТИТЕЛЬНОСТЬ
        Spore,                   // 🌲 Отдельные споры (было Tree)
        SporeCluster,            // 🌳 Кластер спор (было TreeCluster)
        CorruptedVegetation,     // 🌸 Заражённая растительность (было Flower)
        AlienGrowth,             // 🌺 Инопланетный рост (было SmallPlant)
        Forest,                  // 🌲🌲 Лесной массив

        // === ОБЪЕКТЫ НА ПУТЯХ ===
        AbandonedVehicle,        // 🚗 Заброшенная техника 
        SupplyCache,             // 📦 Тайник снабжения 
        Barricade,              // 🚧 Баррикада 
        WreckageDebris,         // 🗑️ Обломки 
        
        // === РЕСУРСЫ EXOFORM ===
        WoodResource,           // 🪵 Древесина
        StoneResource,          // 🪨 Камень
        BiomassResource,        // 🧫 Биомасса
        MetalResource,          // ⚡ Металл
        EnergyResource,         // 🔋 Энергия 
        TechSalvageResource,    // 🔧 Техника для восстановления 
        ArtifactsResource,      // 💎 Артефакты 
        
        // === ЭЛЕМЕНТЫ ПОРЧИ  ===
        TentacleGrowth,         // 🐙 Щупальца Порчи (статичные)
        TumorNode,              // 🧬 Опухолевые узлы (статичные)
        CorruptedGround,        // 🌫️ Заражённая земля
        SporeEmitter,           // 💨 Источник спор
        BiologicalMass,         // 🧬 Биологическая масса
        
        // === ТЕХНИКА ДЛЯ ВОССТАНОВЛЕНИЯ ===
        DamagedGenerator,       // ⚡ Повреждённый генератор
        BrokenRobot,           // 🤖 Сломанный робот
        CorruptedTerminal,     // 💻 Заражённый терминал
        
        // === ДЕКОРАЦИИ ===
        Decoration,            // 🎨 Декоративные объекты
        
        Reserved            // Зарезервировано
    }
}