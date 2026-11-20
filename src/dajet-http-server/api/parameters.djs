
DECLARE @boolean  boolean = TRUE
DECLARE @integer  integer    = 100
DECLARE @big_int  integer(8) = 333
DECLARE @int32_to_int64 integer(8) = 333
DECLARE @decimal      decimal  = 123.00
DECLARE @int32_to_dec decimal  = 321
DECLARE @int64_to_dec decimal  = 9876543210
DECLARE @datetime     datetime = '2026-08-01 12:34:56'
DECLARE @string       string = 'Это строка'
DECLARE @str_date     string = '2027-01-01 12:34:56'
DECLARE @str_uuid     string = '11111111-2222-3333-4444-555555555555'
DECLARE @str_entity   string = '{111:11111111-2222-3333-4444-555555555555}'
DECLARE @binary       binary = 0xDEADBEEF
DECLARE @uuid         uuid   = '11111111-2222-3333-4444-555555555555'
DECLARE @entity       entity = {111:11111111-2222-3333-4444-555555555555}
DECLARE @array_empty  array  = []
DECLARE @array_string array(string) = ['aaa','bbb','ccc']

-- TODO: JSON не поддерживает int64
-- DECLARE @str_bin   binary = '0xDEADBEEF'
-- DECLARE @str_int64 integer(8) = '9876543210'

--PRINT 'boolean = ' + @boolean

RETURN 'SUCCESS'