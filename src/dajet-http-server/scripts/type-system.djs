DECLARE @Булево          boolean  = TRUE
DECLARE @ЦелоеЧисло      integer  = 12345
DECLARE @ДесятичноеЧисло decimal  = 12.34
DECLARE @ДатаВремя       datetime = '2026-04-12T00:00:00'
DECLARE @Строка          string   = 'Это строка'
DECLARE @Идентификатор   uuid     = '643c6b9d-cacf-4048-11f1-3ce54d7b5bf7'
DECLARE @Ссылка          entity   -- Любая ссылка, полученная из базы данных
DECLARE @СоставнойТип    union    -- Составной тип данных (вспомогательный)
DECLARE @Объект          object   -- Объект, полученный запросом из базы данных
DECLARE @Таблица         array    -- Массив объектов

USE 'MS_TEST'
   
   SELECT Булево          = @Булево
        --, ЦелоеЧисло      = @ЦелоеЧисло -- Unable to cast object of type 'System.Decimal' to type 'System.Int32'.
        , ДесятичноеЧисло = @ДесятичноеЧисло
        , ДатаВремя       = @ДатаВремя
        , Строка          = @Строка
        --, Идентификатор   = @Идентификатор -- Unable to cast object of type 'System.Byte[]' to type 'System.Guid'.
        , Ссылка          = @Ссылка
        , СоставнойТип    = @СоставнойТип
     INTO @Объект

END

RETURN @Объект