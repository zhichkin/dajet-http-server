
PRIVATE @Таблица array

USE 'MS_TEST'

  WITH cte AS (SELECT ТипДокумента = TYPEOF(Ссылка), ДокументСсылка = UUIDOF(Ссылка), Ссылка, Номер, Дата
    
    FROM Документ.Приход

  UNION ALL

  SELECT ТипДокумента = TYPEOF(Ссылка), ДокументСсылка = UUIDOF(Ссылка), Ссылка, Номер, Дата
    FROM Документ.Расход
    )
    SELECT ТипДокумента, ДокументСсылка, Ссылка, Номер, Дата INTO @Таблица FROM cte

END

RETURN @Таблица