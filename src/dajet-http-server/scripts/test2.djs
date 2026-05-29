
-- Тест на возврат таблицы в формате JSON
-- Принудительная (явная) сериализация

DECLARE @Код     string
DECLARE @Таблица array

USE 'ms-test'

  SELECT Ссылка, Код, Наименование
    INTO @Таблица
    FROM Справочник.Номенклатура
   WHERE Код = @Код

END

RETURN JSON(@Таблица)