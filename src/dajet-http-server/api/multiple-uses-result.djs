
PRIVATE @Таблица1  array
PRIVATE @Таблица2  array
PRIVATE @Результат object

USE 'MS_UNF'
  SELECT TOP 1 Ссылка, Код, Наименование
    INTO @Таблица1
    FROM Справочник.Номенклатура
END

USE 'PG_UNF'
  SELECT TOP 1 Ссылка, Код, Наименование
    INTO @Таблица2
    FROM Справочник.Номенклатура
END

SET @Результат.MS = @Таблица1
SET @Результат.PG = @Таблица2

RETURN @Результат