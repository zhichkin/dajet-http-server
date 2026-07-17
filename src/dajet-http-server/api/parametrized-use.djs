
DECLARE @БазаДанных string
DECLARE @КодТовара  string
PRIVATE @Таблица    array

USE @БазаДанных

  SELECT Ссылка, Код, Наименование
    INTO @Таблица
    FROM Справочник.Номенклатура
   WHERE Код = @КодТовара

END

RETURN @Таблица