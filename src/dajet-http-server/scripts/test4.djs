
-- Тест на возврат объекта в формате JSON
-- Автоматическая (неявная) сериализация

DECLARE @Код    string
DECLARE @Объект object

USE 'ms-test'

  SELECT TOP 1 Ссылка, Код, Наименование
    INTO @Объект
    FROM Справочник.Номенклатура
   WHERE Код = @Код

END

RETURN @Объект