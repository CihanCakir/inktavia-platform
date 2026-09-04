-- LT yük testi verisi temizliği: yalnız LOADTEST- önekli kayıtlar.
-- Koşum (lokal):  docker exec -i postgres psql -U aizen -d inktavia_store -f - < load-test/cleanup.sql
-- Önce say, sonra sil — sayım beklenenle uyuşmuyorsa SİLME, incele.
SELECT count(*) AS loadtest_products FROM cargodry.products WHERE "ProductCode" LIKE 'LOADTEST-%';
DELETE FROM cargodry.products WHERE "ProductCode" LIKE 'LOADTEST-%';
