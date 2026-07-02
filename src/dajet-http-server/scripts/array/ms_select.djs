
DECLARE @МассивКодов array(string) = ["ФР-00000484","ФР-00000485"]

PRIVATE @Таблица array

USE 'MS_UNF'

  SELECT СУБД = 'MS', Ссылка, Код, Наименование
    INTO @Таблица
    FROM Справочник.Номенклатура
   WHERE Код IN (@МассивКодов)
   ORDER BY Код DESC

END

RETURN @Таблица