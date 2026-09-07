# Команды управления последовательностью

[Подробная документация](https://zhichkin.github.io/dajet-script/sequence/)

В данной статье приводятся примеры использования для DaJet Script 2.0

1. Создание последовательности.

```SQL
USE 'MS_TEST'

  CREATE SEQUENCE so_import

END

RETURN 'Sequence [so_import] created successfully'
```

2. Функция ```VECTOR```.

```SQL
PRIVATE @vector decimal

USE 'MS_TEST'

  SELECT VECTOR('so_import') INTO @vector

END

RETURN 'Current sequence value is ' + @vector
```

3. Применение последовательности.

```SQL
USE 'MS_TEST'

  APPLY SEQUENCE so_import ON РегистрСведений.ИсходящаяОчередь(НомерСообщения) -- RECALCULATE

END

RETURN 'Sequence [so_import] applied successfully'
```

4. Отзыв последовательности.

```SQL
USE 'MS_TEST'

  REVOKE SEQUENCE so_import ON РегистрСведений.ИсходящаяОчередь

END

RETURN 'Sequence [so_import] revoked successfully'
```

5. Удаление последовательности.


```SQL
USE 'MS_TEST'

  DROP SEQUENCE so_import

END

RETURN 'Sequence [so_import] dropped successfully'
```
