namespace PandemicWars.Scripts.Map
{
    public enum TileType
    {
        Grass,              // Трава
        RoadStraight,       // Прямая дорога
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

        Reserved            // Зарезервировано
    }
}