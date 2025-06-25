namespace PandemicWars.Scripts.Map
{
    public enum TileType
    {
        // Базовые типы
        Grass,              // Трава
        RoadStraight,       // Прямая дорога
        
        // Здания
        Building,           // Здание
        LargeBuilding,      // Крупное здание
        Mall,               // Торговый центр
        Factory,            // Завод
        Park,               // Парк
        Special,            // Особый объект
        Infrastructure,     // Инфраструктура

        // Растительность
        Tree,               // Отдельные деревья
        TreeCluster,        // Группы деревьев
        Bush,               // Кусты
        Flower,             // Цветы
        SmallPlant,         // Мелкие растения

        // Специальные зоны
        Forest,             // Лесные массивы
        Garden,             // Сады

        // Объекты на дорогах
        BrokenCar,          // Сломанная машина
        Loot,               // Лут/припасы
        Roadblock,          // Блокпост/баррикада
        Debris,             // Обломки/мусор
        
        // Декорации
        Decoration,         // Декоративные объекты
        
        // НОВОЕ: Ресурсы
        WoodResource,       // Древесина
        StoneResource,      // Камень
        FoodResource,       // Еда/сельхоз
        MetalResource,      // Металл/руда
        
        Reserved            // Зарезервировано
    }
}