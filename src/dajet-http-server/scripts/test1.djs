
-- Тест на возврат таблицы в формате JSON
-- Автоматическая (неявная) сериализация

DECLARE @Код     string
DECLARE @Таблица array

USE 'ms-test'

  SELECT Ссылка, Код, Наименование
    INTO @Таблица
    FROM Справочник.Номенклатура
   WHERE Код = @Код

END

RETURN @Таблица