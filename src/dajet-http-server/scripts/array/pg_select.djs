
DECLARE @МассивКодов array(string)

PRIVATE @Таблица array

USE 'PG_UNF'

  SELECT СУБД = 'PG', Ссылка, Код, Наименование
    INTO @Таблица
    FROM Справочник.Номенклатура
   WHERE Код IN (@МассивКодов)
   ORDER BY Код

END

RETURN @Таблица