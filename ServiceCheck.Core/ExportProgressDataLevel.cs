namespace ServiceCheck.Core
{
    public enum ExportProgressDataLevel
    {
        /// <summary>
        /// Не использовать при создании события, техническое значение
        /// </summary>
        NONE = 0,
        /// <summary>
        /// Ошибки
        /// </summary>
        ERROR = 1,

        /// <summary>
        /// Начало какого-либо достаточного для измерения его длительности этапа
        /// </summary>
        STAGESTARTINFO = 2,
        /// <summary>
        /// Завершение какого-либо достаточного для измерения его длительности этапа
        /// </summary>
        STAGEENDINFO = 3,
        /// <summary>
        /// Моментальное, не длящееся во времени событие
        /// </summary>
        MOMENTALEVENTINFO = 4,

        /// <summary>
        /// Отмена пользователем
        /// </summary>
        CANCEL = 5,
        /// <summary>
        /// Не критичное, но требуеющее внимания событие
        /// </summary>
        WARNING = 6
    }
}