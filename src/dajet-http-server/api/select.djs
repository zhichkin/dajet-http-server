
DECLARE @Код string
PRIVATE @Таблица array

USE 'MS_UNF'

  SELECT Ссылка, Код, Наименование
    INTO @Таблица
    FROM Справочник.Номенклатура
   WHERE Код = @Код

  --DELETE Справочник.Номенклатура

END

RETURN @Таблица