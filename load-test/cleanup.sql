-- LT yük testi verisi temizliği: yalnız LOADTEST- önekli kayıtlar.
-- Koşum (lokal):  docker exec -i postgres psql -U aizen -d inktavia_store -f - < load-test/cleanup.sql
-- Önce say, sonra sil — sayım beklenenle uyuşmuyorsa SİLME, incele.
SELECT count(*) AS loadtest_products FROM cargodry.products WHERE "ProductCode" LIKE 'LOADTEST-%';
DELETE FROM cargodry.products WHERE "ProductCode" LIKE 'LOADTEST-%';

-- ============================================================================
-- s7 — servis talepleri (SR). s7 talepleri KENDİ İPTAL eder (Cancelled = terminal), yani discovery'yi
-- kirletmezler; bu sert silme İSTEĞE BAĞLI (DB'yi tamamen boşaltmak için).
-- FK'ler: service_requests'e bağlı çocukların ÇOĞU ON DELETE CASCADE; yalnız 3'ü RESTRICT
-- (service_request_assignments / _completions / _disputes) + work_logs. Önce onları sil, sonra
-- parent silinince gerisi (offers, items, messages, trips, change_orders, conversations…) cascade gider.
-- Hepsi tek transaction — yanlış bir ad varsa rollback, kısmi silme OLMAZ. Önce say:
SELECT count(*) AS loadtest_service_requests FROM servicerequest.service_requests WHERE "Title" LIKE 'LOADTEST-%';

BEGIN;
  DELETE FROM servicerequest.service_request_work_logs
    WHERE "ServiceRequestId" IN (SELECT "Id" FROM servicerequest.service_requests WHERE "Title" LIKE 'LOADTEST-%');
  DELETE FROM servicerequest.service_request_assignments
    WHERE "ServiceRequestId" IN (SELECT "Id" FROM servicerequest.service_requests WHERE "Title" LIKE 'LOADTEST-%');
  DELETE FROM servicerequest.service_request_completions
    WHERE "ServiceRequestId" IN (SELECT "Id" FROM servicerequest.service_requests WHERE "Title" LIKE 'LOADTEST-%');
  DELETE FROM servicerequest.service_request_disputes
    WHERE "ServiceRequestId" IN (SELECT "Id" FROM servicerequest.service_requests WHERE "Title" LIKE 'LOADTEST-%');
  DELETE FROM servicerequest.service_requests WHERE "Title" LIKE 'LOADTEST-%';   -- cascade: gerisi
COMMIT;

-- ============================================================================
-- s6 / s7 — tekneler. s6 KENDİ ARŞİVLER (soft-delete); bu sert silme İSTEĞE BAĞLI.
-- vessel şemasındaki çocukların hepsi "VesselId" ile bağlı — önce çocuklar, sonra vessels. Önce say:
SELECT count(*) AS loadtest_vessels FROM vessel.vessels WHERE "Name" LIKE 'LOADTEST-%';

BEGIN;
  DELETE FROM vessel.vessel_engines            WHERE "VesselId" IN (SELECT "Id" FROM vessel.vessels WHERE "Name" LIKE 'LOADTEST-%');
  DELETE FROM vessel.vessel_specifications     WHERE "VesselId" IN (SELECT "Id" FROM vessel.vessels WHERE "Name" LIKE 'LOADTEST-%');
  DELETE FROM vessel.vessel_owners             WHERE "VesselId" IN (SELECT "Id" FROM vessel.vessels WHERE "Name" LIKE 'LOADTEST-%');
  DELETE FROM vessel.vessel_media              WHERE "VesselId" IN (SELECT "Id" FROM vessel.vessels WHERE "Name" LIKE 'LOADTEST-%');
  DELETE FROM vessel.vessel_status_histories   WHERE "VesselId" IN (SELECT "Id" FROM vessel.vessels WHERE "Name" LIKE 'LOADTEST-%');
  DELETE FROM vessel.vessel_documents          WHERE "VesselId" IN (SELECT "Id" FROM vessel.vessels WHERE "Name" LIKE 'LOADTEST-%');
  DELETE FROM vessel.vessel_location_snapshots WHERE "VesselId" IN (SELECT "Id" FROM vessel.vessels WHERE "Name" LIKE 'LOADTEST-%');
  DELETE FROM vessel.vessels WHERE "Name" LIKE 'LOADTEST-%';
COMMIT;
