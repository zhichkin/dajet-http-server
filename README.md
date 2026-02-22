# DaJet HTTP Server

Web API сервер DaJet для получения информации о метаданных 1С:Предприятие 8 из разных баз данных.

В качестве провайдеров данных (свойство "DataSource" HTTP-запросов) поддерживается только два значения ```SqlServer``` и ```PostgreSql```.

#### GET ```/status```

Получает информацию о сервере DaJet.

**Запрос**
```
curl -X GET http://localhost:5000/status
```

**Ответ**
```
{
  "Name": "DaJet.Http.Server",
  "Version": "4.0.0",
  "ServerTime": "2026-02-22 16:28:11"
}
```

#### GET ```/```

Получает список, зарегистрированных на сервере DaJet, баз данных.

**Запрос**
```
curl -X GET http://localhost:5000/
```

**Ответ**
```
[
  {
    "Name": "MS_ERP",
    "DataSource": "SqlServer",
    "ConnectionString": "Data Source=server;Initial Catalog=erp;Integrated Security=True;Encrypt=False;",
    "LastUpdated": 5,
    "IsInitialized": true
  },
  {
    "Name": "MS_UNF",
    "DataSource": "SqlServer",
    "ConnectionString": "Data Source=server;Initial Catalog=unf;Integrated Security=True;Encrypt=False;",
    "LastUpdated": 0,
    "IsInitialized": false
  }
]
```

#### PUT ```/```

Регистрирует новую базу данных на сервере DaJet.

**Запрос**
```
curl -v -X PUT -H "Content-Type: application/json; charset=utf-8" -d @body.json http://localhost:5000/
```

Тело запроса в кодировке UTF-8, файл body.json
```
{
  "name": "PG_UNF",
  "type": "PostgreSql",
  "path": "Host=localhost;Port=5432;Database=unf;Username=postgres;Password=postgres;"
}
```

**Ответ**
```
< HTTP/1.1 201 Created
< Content-Length: 0
< Date: Sun, 22 Feb 2026 13:36:27 GMT
< Server: Kestrel
```

#### PATCH ```/```

Сбрасывает кэш метаданных, а затем обновляет по имени информацию о провайдере и строке подключения к базе данных на сервере DaJet.

**Запрос**
```
curl -v -X PATCH -H "Content-Type: application/json; charset=utf-8" -d @body.json http://localhost:5000/
```

Тело запроса в кодировке UTF-8, файл body.json
```
{
  "name": "PG_UNF",
  "type": "PostgreSql",
  "path": "Host=127.0.0.1;Port=5432;Database=unf;Username=postgres;Password=postgres;"
}
```

**Ответ**
```
< HTTP/1.1 200 OK
< Content-Length: 0
< Date: Sun, 22 Feb 2026 13:44:02 GMT
< Server: Kestrel
```

#### DELETE ```/```

Удаляет регистрацию базы данных на сервере DaJet.

**Запрос**
```
curl -v -X DELETE http://localhost:5000/MS_TEST
```

**Ответ**
```
< HTTP/1.1 200 OK
< Content-Length: 0
< Date: Sun, 22 Feb 2026 13:48:53 GMT
< Server: Kestrel
```

#### POST ```/reset/{database}```

Принудительно обновляет (перезагружает) кэш метаданных для указанной базы данных на сервере DaJet.

**Запрос**
```
curl -v -X POST http://localhost:5000/reset/PG_UNF
```

**Ответ**
```
< HTTP/1.1 200 OK
< Content-Length: 0
< Date: Sun, 22 Feb 2026 13:57:22 GMT
< Server: Kestrel
```
