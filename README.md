# DaJet HTTP Server <a href="https://hub.docker.com/r/zhichkin/dajet-http-server"><img width="32" height="32" alt="docker-logo" src="https://github.com/user-attachments/assets/e41122f3-8aae-4ea0-9bb3-289b874b5c4c" /></a>

[Документация](/doc)

WEB API сервер для хостинга сервисов DaJet.

### Установка и запуск на Windows или Linux

1. Установить [Microsoft .NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
2. Скачать дистрибутив [DaJet HTTP Server](https://github.com/zhichkin/dajet-http-server/releases/latest)
3. Создать рабочий каталог и распаковать в него дистрибутив, например: ```C:\dajet-http-server```
4. Перейти в каталог установки и запустить исполняемый файл ```DaJet.Http.Server.exe```
5. Открыть браузер и перейти по адресу ```http://localhost:5000/status```

<img width="627" height="411" alt="image" src="https://github.com/user-attachments/assets/9ad392ac-844d-4d2f-8c5d-7f87813f1414" />

<img width="471" height="186" alt="image" src="https://github.com/user-attachments/assets/d97f21b3-09ca-4ec0-b9f3-e5fd26e865bc" />

### Установка и запуск в Docker

1. Получить образ из Docker Hub

```
docker pull zhichkin/dajet-http-server
```

2. Запустить контейнер в Docker

```
docker run --name dajet-http-server --user=root -it -p 5000:5000 zhichkin/dajet-http-server
```

3. Открыть браузер и перейти по адресу ```http://localhost:5000/status```
