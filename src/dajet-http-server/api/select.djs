
DECLARE @Код string
PRIVATE @Таблица array

USE 'MS_UNF'

  SELECT Ссылка, Код, Наименование
    INTO @Таблица
    FROM Справочник.Номенклатура
   WHERE Код = @Код

  --IF @Код = '' THEN RETURN @Таблица ELSE RETURN '@Код is empty' END

END

RETURN @Таблица