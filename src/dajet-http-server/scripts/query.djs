
DECLARE @Код string
PRIVATE @Таблица array

USE 'MS_UNF'

  WITH Выборка AS
  (
    SELECT TOP 5
           НомерПоПорядку = ROW_NUMBER() OVER (ORDER BY Код)
         , Ссылка
         , Наименование
      FROM Справочник.Номенклатура
     ORDER BY НомерПоПорядку ASC
  )
  SELECT НомерПоПорядку, Ссылка, Наименование
    INTO @Таблица
    FROM Выборка
   WHERE НомерПоПорядку <= 5

END

RETURN @Таблица