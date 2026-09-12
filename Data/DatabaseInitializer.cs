using GeniusVendas.Api.Services;
using Npgsql;

namespace GeniusVendas.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(DatabaseConnectionFactory factory, PasswordHasher hasher, ILogger logger)
    {
        await using var conn = factory.Create();
        await conn.OpenAsync();

        var sql = @"
CREATE TABLE IF NOT EXISTS company(
 id BIGSERIAL PRIMARY KEY,
 name VARCHAR(150) NOT NULL,
 cnpj VARCHAR(14),
 integration_key VARCHAR(100) NOT NULL UNIQUE,
 active BOOLEAN NOT NULL DEFAULT TRUE,
 created_at_utc TIMESTAMP NOT NULL DEFAULT (now() at time zone 'utc')
);
CREATE TABLE IF NOT EXISTS seller(
 id BIGSERIAL PRIMARY KEY,
 company_id BIGINT NOT NULL REFERENCES company(id),
 gdoor_code VARCHAR(30) NOT NULL,
 name VARCHAR(150) NOT NULL,
 active BOOLEAN NOT NULL DEFAULT TRUE,
 UNIQUE(company_id,gdoor_code)
);
CREATE TABLE IF NOT EXISTS app_user(
 id BIGSERIAL PRIMARY KEY,
 company_id BIGINT NOT NULL REFERENCES company(id),
 seller_id BIGINT NOT NULL REFERENCES seller(id),
 username VARCHAR(80) NOT NULL,
 password_hash TEXT NOT NULL,
 active BOOLEAN NOT NULL DEFAULT TRUE,
 UNIQUE(company_id,username)
);
CREATE TABLE IF NOT EXISTS app_session(
 token VARCHAR(128) PRIMARY KEY,
 user_id BIGINT NOT NULL REFERENCES app_user(id) ON DELETE CASCADE,
 expires_at_utc TIMESTAMP NOT NULL
);
CREATE TABLE IF NOT EXISTS product(
 id BIGSERIAL PRIMARY KEY,
 company_id BIGINT NOT NULL REFERENCES company(id),
 gdoor_code VARCHAR(30) NOT NULL,
 barcode VARCHAR(30) NOT NULL DEFAULT '',
 description VARCHAR(200) NOT NULL,
 retail_price NUMERIC(18,4) NOT NULL DEFAULT 0,
 wholesale_price NUMERIC(18,4) NOT NULL DEFAULT 0,
 wholesale_min_qty NUMERIC(18,4) NOT NULL DEFAULT 0,
 stock NUMERIC(18,4) NOT NULL DEFAULT 0,
 active BOOLEAN NOT NULL DEFAULT TRUE,
 updated_at_utc TIMESTAMP NOT NULL DEFAULT (now() at time zone 'utc'),
 UNIQUE(company_id,gdoor_code)
);
CREATE INDEX IF NOT EXISTS ix_product_company_description ON product(company_id,description);
CREATE INDEX IF NOT EXISTS ix_product_company_barcode ON product(company_id,barcode);
CREATE TABLE IF NOT EXISTS customer(
 id BIGSERIAL PRIMARY KEY,
 company_id BIGINT NOT NULL REFERENCES company(id),
 gdoor_code VARCHAR(30) NOT NULL,
 name VARCHAR(200) NOT NULL,
 document VARCHAR(20) NOT NULL DEFAULT '',
 active BOOLEAN NOT NULL DEFAULT TRUE,
 updated_at_utc TIMESTAMP NOT NULL DEFAULT (now() at time zone 'utc'),
 UNIQUE(company_id,gdoor_code)
);
CREATE INDEX IF NOT EXISTS ix_customer_company_name ON customer(company_id,name);
CREATE TABLE IF NOT EXISTS sales_order(
 id BIGSERIAL PRIMARY KEY,
 company_id BIGINT NOT NULL REFERENCES company(id),
 external_id UUID NOT NULL UNIQUE,
 customer_id BIGINT NOT NULL REFERENCES customer(id),
 seller_id BIGINT NOT NULL REFERENCES seller(id),
 order_date_utc TIMESTAMP NOT NULL,
 total NUMERIC(18,4) NOT NULL,
 status VARCHAR(30) NOT NULL,
 gdoor_order_number VARCHAR(30),
 error_message TEXT,
 sent_to_gdoor_at_utc TIMESTAMP NULL
);
CREATE INDEX IF NOT EXISTS ix_order_company_status ON sales_order(company_id,status);
CREATE TABLE IF NOT EXISTS sales_order_item(
 id BIGSERIAL PRIMARY KEY,
 order_id BIGINT NOT NULL REFERENCES sales_order(id) ON DELETE CASCADE,
 product_id BIGINT NOT NULL REFERENCES product(id),
 product_code VARCHAR(30) NOT NULL,
 description VARCHAR(200) NOT NULL,
 quantity NUMERIC(18,4) NOT NULL,
 unit_price NUMERIC(18,4) NOT NULL,
 wholesale BOOLEAN NOT NULL,
 total NUMERIC(18,4) NOT NULL
);";
        await using (var cmd = new NpgsqlCommand(sql, conn)) await cmd.ExecuteNonQueryAsync();

        var countCmd = new NpgsqlCommand("SELECT COUNT(*) FROM company", conn);
        var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
        if (count == 0)
        {
            var key = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
            await using var tx = await conn.BeginTransactionAsync();
            long companyId;
            await using (var cmd = new NpgsqlCommand("INSERT INTO company(name,cnpj,integration_key) VALUES('CLIENTE PILOTO','',@k) RETURNING id", conn, tx))
            { cmd.Parameters.AddWithValue("k", key); companyId = Convert.ToInt64(await cmd.ExecuteScalarAsync()); }
            long sellerId;
            await using (var cmd = new NpgsqlCommand("INSERT INTO seller(company_id,gdoor_code,name) VALUES(@c,'ANTONIO','Antonio') RETURNING id", conn, tx))
            { cmd.Parameters.AddWithValue("c", companyId); sellerId = Convert.ToInt64(await cmd.ExecuteScalarAsync()); }
            await using (var cmd = new NpgsqlCommand("INSERT INTO app_user(company_id,seller_id,username,password_hash) VALUES(@c,@s,'antonio',@p)", conn, tx))
            { cmd.Parameters.AddWithValue("c", companyId); cmd.Parameters.AddWithValue("s", sellerId); cmd.Parameters.AddWithValue("p", hasher.Hash("1234")); await cmd.ExecuteNonQueryAsync(); }
            await tx.CommitAsync();
            logger.LogWarning("Banco inicializado. Login piloto: antonio / 1234. Integration key: {Key}. ALTERE AS CREDENCIAIS APÓS O PRIMEIRO TESTE.", key);
        }
    }
}
