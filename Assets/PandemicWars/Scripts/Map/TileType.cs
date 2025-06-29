namespace PandemicWars.Scripts.Map
{
    public enum TileType
    {
        // Базовые типы
        Grass,              // Трава
        RoadStraight,       // Прямая дорога
        
        // Здания
        Structure,           
        LargeStructure,      
        ResearchFacility,          // исследовательские объекты
        ContainmentUnit,           // блоки сдерживания
        BioDome,            // био-купола
        CommandCenter,            // Особый объект

        // Растительность
        Spore,               // Отдельные споры
        SporeCluster,               // Кусты
        CorruptedVegetation,         // Мелкие растения

        // Специальные зоны
        Forest,             // Лесные массивы
        AlienGrowth,             // инопланетный рост

        // Объекты на дорогах
        BrokenCar,          // Сломанная машина
        SupplyCache,               // тайники снабжения
        Roadblock,          // Блокпост/баррикада
        Debris,             // Обломки/мусор
        
        // Декорации
        Decoration,         // Декоративные объекты
        
        //Ресурсы
        WoodResource,       // Древесина
        StoneResource,      // Камень
        BiomassResource,       // биомасса 
        MetalResource,      // Металл/руда
        EnergyResource,         // энергия для активации
        ArtifactsResource,      // артефакты
        TechSalvageResource,    // технические обломки
        
        Reserved            // Зарезервировано
    }
}