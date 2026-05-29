# Выполнение произвольных параметризованных запросов

#### POST ```/query```

Endpoint для выполнения произвольных параметризованных запросов SELECT.

Результат запроса возвращается как массив объектов в формате JSON.

**Файл тела запроса ```query.json```**

```JSON
{
  "database": "MS_TEST",
  "script": "SELECT Ссылка, Наименование FROM Справочник.Номенклатура WHERE Код = @Код",
  "parameters":
  {
    "Код": "1234567890"
  }
}
```

**Запрос**
```
curl -v -X POST http://localhost:5000/query -d @query.json -H "Content-Type: application/json; charset=utf-8"
```

**Ответ**
```
{
  "success": true,
  "message": "",
  "result": "[{\u0022Ссылка\u0022:\u0022{927:643c709d-cacf-4048-11f1-4f9984f0927d}\u0022,\u0022Код\u0022:\u00221234567890\u0022,\u0022Наименование\u0022:\u0022Товар 1234567890\u0022}]"
}
```

**Удобочитаемое значение свойства ```result```**

```JSON
[
  {
    "Ссылка": "{927:643c709d-cacf-4048-11f1-4f9984f0927d}",
    "Код": "1234567890",
    "Наименование": "Товар 1234567890"
  }
]
```

#### JSON Schema тела запроса

```JSON
{
  "type": "object",
  "properties": {
    "database": {
      "type": "string"
    },
    "script": {
      "type": "string"
    },
    "parameters": {
      "type": "object"
    }
  },
  "required": [ "database", "script", "parameters" ]
}
```

#### JSON Schema тела ответа

```JSON
{
  "type": "object",
  "properties": {
    "success": {
      "type": "boolean"
    },
    "message": {
      "type": "string"
    },
    "result": {
      "type": "array",
      "items": { "type": "object" }
    }
  },
  "required": [ "success", "message", "result" ]
}
```

#### Поддерживаемые типы параметров запроса

```JSON
{
  "Булево": true,
  "Число": 1234,
  "ДатаВремя": "2026-01-01T12:34:56",
  "Строка": "Это строка",
  "Идентификатор": "1677349A-095F-4488-896F-93425B720FEB",
  "Ссылка": "{333:1677349A-095F-4488-896F-93425B720FEB}"
}
```
