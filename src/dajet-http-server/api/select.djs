
DECLARE @Код string
PRIVATE @Таблица array

USE 'MS_UNF'

  SELECT Ссылка, Код, Наименование
    INTO @Таблица
    FROM Справочник.Номенклатура
   WHERE Код = @Код

  --DELETE Справочник.Номенклатура

  --IF TRUE THEN PRINT 'TRUE' ELSE PRINT 'FALSE' END

END

RETURN @Таблица