# Получение метаданных 1С:Предприятие 8

#### GET ```/md```

Получает массив имён, зарегистрированных на сервере DaJet, баз данных.

**Запрос**
```
curl -X GET http://localhost:5000/md
```

**Ответ**
```
[
  "PG_UNF",
  "MS_ERP",
  "MS_UNF",
  "MS_TEST"
]
```

#### GET ```/md/{database}```

Получает список конфигураций базы данных (основная и расширения).

**Запрос**
```
curl -X GET http://localhost:5000/md/MS_TEST
```

**Ответ**
```
[
  {
    "Uuid": "684c8f2b-d93f-49cc-b766-b3cc3896b047",
    "Name": "DaJet_Metadata",
    "NamePrefix": "",
    "YearOffset": 0,
    "AppVersion": "1.0.0",
    "PlatformVersion": 80327
  },
  {
    "Uuid": "9b7ed7bd-6634-4d0e-988b-38924330fd6f",
    "Name": "Расширение1",
    "NamePrefix": "Расш1_",
    "YearOffset": 0,
    "AppVersion": "1.0.0",
    "PlatformVersion": 80327
  }
]
```

#### GET ```/md/names/{database}/{type}```

Получает список имён объектов метаданных указанного типа, например "Справочник".

**Поддерживаемые значения для параметра ```{type}```**

<table>
  <tr><td>Константа</td><td>Справочник</td><td>Документ</td></tr>
  <tr><td>ПланВидовХарактеристик</td><td>ПланОбмена</td><td>ПланСчетов</td></tr>
  <tr><td>РегистрСведений</td><td>РегистрНакопления</td><td>РегистрБухгалтерии</td></tr>
  <tr><td>Задача</td><td>БизнесПроцесс</td><td></td></tr>
</table>


**Запрос**
```
curl -X GET http://localhost:5000/md/names/MS_TEST/Справочник
```

**Ответ**
```
[
  "Справочник1",
  "Справочник2",
  "Заимствованный",
  "Расш1_Справочник1",
  "Расш1_Справочник2"
]
```

#### GET ```/md/names/{database}/{configuration}/{type}```

Получает список имён объектов метаданных, принадлежащих указанной конфигурации.

**Запрос**
```
curl -X GET http://localhost:5000/md/names/MS_TEST/Расширение1/Справочник
```

**Ответ**
```
[
  "Заимствованный",
  "Расш1_Справочник1",
  "Расш1_Справочник2"
]
```

#### GET ```/md/entity/{database}/{type}/{name}```

Получает объект метаданных по его полному имени.

**Запрос**
```
curl -X GET http://localhost:5000/md/entity/MS_TEST/Справочник/Расш1_Справочник1
```

**Ответ**
```JSON
{
  "Name": "Расш1_Справочник1",
  "DbName": "_Reference712x1",
  "Properties": [
    {
      "Name": "Ссылка",
      "Type": "entity(712)",
      "Purpose": "System",
      "Columns": [
        {
          "Name": "_IDRRef",
          "Type": "binary(16,fixed)",
          "Purpose": "Value"
        }
      ],
      "References": []
    },
// ...
```

#### GET ```/md/entity/{database}/{code:int}```

Получает объект метаданных по его коду типа.

**Запрос**
```
curl -X GET http://localhost:5000/md/entity/MS_TEST/712
```

**Ответ**
```JSON
{
  "Name": "Расш1_Справочник1",
  "DbName": "_Reference712x1",
  "Properties": [
    {
      "Name": "Ссылка",
      "Type": "entity(712)",
      "Purpose": "System",
      "Columns": [
        {
          "Name": "_IDRRef",
          "Type": "binary(16,fixed)",
          "Purpose": "Value"
        }
      ],
      "References": []
    },
// ...
```

#### GET ```/md/references/{database}```

Расшифровывает ссылки свойства "References" объекта метаданных.

При запросе объекта метаданных ссылочные свойства этого объекта могут иметь следующий вид:

```JSON
// ...
 {
      "Name": "Реквизит1",
      "Type": "entity",
      "Purpose": "Property",
      "Columns": [
        {
          "Name": "_Fld717_TYPE",
          "Type": "binary(1,fixed)",
          "Purpose": "Tag"
        },
        {
          "Name": "_Fld717_RTRef",
          "Type": "binary(4,fixed)",
          "Purpose": "TypeCode"
        },
        {
          "Name": "_Fld717_RRRef",
          "Type": "binary(16,fixed)",
          "Purpose": "Identity"
        }
      ],
      "References": [
        "0843611e-1ba8-4e5b-8e2f-9bd97b1209fe",
        "d518144c-4536-4a67-8d07-cb18e3cfa172"
      ]
    }
// ...
```

Для расшифровки значений свойства "References" можно использовать следующий запрос.

**Запрос**
```
curl -v -X GET -H "Content-Type: application/json; charset=utf-8" -d @references.json http://localhost:5000/md/references/MS_TEST
```

Тело запроса в кодировке UTF-8, файл references.json

```
[
  "0843611e-1ba8-4e5b-8e2f-9bd97b1209fe",
  "d518144c-4536-4a67-8d07-cb18e3cfa172"
]
```

**Ответ**
```
[
  "Справочник.Расш1_Справочник2",
  "Справочник.Заимствованный"
]
```
