using Microsoft.AspNetCore.Mvc;
using TiendaWeb.Data;
using TiendaWeb.Models;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace TiendaWeb.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;

        public CategoryController(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            ViewBag.ConnectionMessage = "✅ CONEXIÓN EXITOSA";

            try
            {
                // SOLUCIÓN DE EMERGENCIA: SQL DIRECTO
                var connectionString = _configuration.GetConnectionString("ISCDApplications_DevConn");
                var categories = new List<Category>();

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // PRIMERO: Verificar qué columnas existen realmente
                    var cmdCheck = new SqlCommand(@"
                        SELECT COLUMN_NAME 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Category' 
                        ORDER BY ORDINAL_POSITION", connection);

                    var columns = new List<string>();
                    using (var reader = cmdCheck.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            columns.Add(reader.GetString(0));
                        }
                    }

                    ViewBag.ActualColumns = string.Join(", ", columns);

                    // SEGUNDO: Construir consulta dinámica
                    string selectQuery;
                    if (columns.Contains("DisplayOrder"))
                    {
                        selectQuery = "SELECT Id, Name, DisplayOrder FROM Category";
                    }
                    else if (columns.Contains("CategoryId"))
                    {
                        selectQuery = "SELECT Id, Name, CategoryId FROM Category";
                    }
                    else
                    {
                        // Consulta genérica
                        var columnList = string.Join(", ", columns.Select(c => $"[{c}]"));
                        selectQuery = $"SELECT {columnList} FROM Category";
                    }

                    // TERCERO: Ejecutar consulta
                    var cmdData = new SqlCommand(selectQuery, connection);
                    using (var reader = cmdData.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var category = new Category();

                            // Mapeo dinámico
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var columnName = reader.GetName(i);
                                var value = reader.GetValue(i);

                                if (columnName == "Id")
                                    category.Id = Convert.ToInt32(value);
                                else if (columnName == "Name")
                                    category.Name = value.ToString();
                                else if (columnName == "DisplayOrder" || columnName == "CategoryId")
                                    category.DisplayOrder = Convert.ToInt32(value);
                            }

                            categories.Add(category);
                        }
                    }

                    connection.Close();
                }

                ViewBag.RawCount = $"{categories.Count} categorías cargadas directamente desde SQL";
                return View(categories);
            }
            catch (Exception ex)
            {
                ViewBag.ConnectionMessage = $"❌ ERROR: {ex.Message}";
                return View(new List<Category>());
            }
        }

        // Método para RECREAR la base de datos CORRECTAMENTE
        public IActionResult RecreateDatabase()
        {
            var result = ""; // ← DEBE estar declarado aquí

            try
            {
                var connectionString = _configuration.GetConnectionString("ISCDApplications_DevConn");

                using (var connection = new SqlConnection(connectionString.Replace("Database=DevWeb", "Database=master")))
                {
                    connection.Open();

                    result = "=== RECREANDO BASE DE DATOS ===\n\n";

                    // 1. Eliminar si existe
                    result += "1. Eliminando base de datos antigua...\n";
                    try
                    {
                        var cmdDrop = new SqlCommand(
                            "IF DB_ID('DevWeb') IS NOT NULL BEGIN " +
                            "ALTER DATABASE DevWeb SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                            "DROP DATABASE DevWeb; END", connection);
                        cmdDrop.ExecuteNonQuery();
                        result += "   ✅ Base de datos eliminada\n";
                    }
                    catch (Exception ex)
                    {
                        result += $"   ⚠️ Error al eliminar: {ex.Message}\n";
                    }

                    // 2. Crear nueva
                    result += "\n2. Creando nueva base de datos...\n";
                    var cmdCreate = new SqlCommand("CREATE DATABASE DevWeb", connection);
                    cmdCreate.ExecuteNonQuery();
                    result += "   ✅ Base de datos creada\n";

                    connection.Close();
                }

                // 3. Crear tabla con estructura CORRECTA
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    result += "\n3. Creando tabla con estructura CORRECTA...\n";
                    var cmdTable = new SqlCommand(@"
                        CREATE TABLE Category (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            Name NVARCHAR(50) NOT NULL,
                            DisplayOrder INT NOT NULL
                        )
                        
                        -- Insertar datos de prueba
                        INSERT INTO Category (Name, DisplayOrder) VALUES
                        ('Electrónica', 1),
                        ('Ropa', 2),
                        ('Libros', 3),
                        ('Hogar', 4),
                        ('Deportes', 5)
                    ", connection);

                    cmdTable.ExecuteNonQuery();
                    result += "   ✅ Tabla creada con datos de prueba\n";

                    connection.Close();
                }

                result += "\n🎉 BASE DE DATOS RECREADA EXITOSAMENTE\n\n";
                result += "<a href='/Category'>Ir a Category List</a>";

                return Content(result.Replace("\n", "<br>"));
            }
            catch (Exception ex)
            {
                return Content($"❌ ERROR: {ex.Message}<br><br>{ex.StackTrace}".Replace("\n", "<br>"));
            }
        }

        // Método para VER columnas actuales
        public IActionResult ShowColumns()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("ISCDApplications_DevConn");

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var cmd = new SqlCommand(@"
                        SELECT 
                            COLUMN_NAME,
                            DATA_TYPE,
                            IS_NULLABLE,
                            COLUMNPROPERTY(OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME), COLUMN_NAME, 'IsIdentity') AS IsIdentity
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Category'
                        ORDER BY ORDINAL_POSITION", connection);

                    var result = "<h3>Columnas de la tabla Category:</h3><table border='1'><tr><th>Columna</th><th>Tipo</th><th>Nulo</th><th>Identity</th></tr>";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result += "<tr>";
                            result += $"<td>{reader.GetString(0)}</td>";
                            result += $"<td>{reader.GetString(1)}</td>";
                            result += $"<td>{reader.GetString(2)}</td>";
                            result += $"<td>{(reader.GetInt32(3) == 1 ? "Sí" : "No")}</td>";
                            result += "</tr>";
                        }
                    }

                    result += "</table>";

                    // Ver datos
                    result += "<h3>Datos actuales:</h3>";
                    var cmdData = new SqlCommand("SELECT * FROM Category", connection);
                    using (var reader = cmdData.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            result += "<table border='1'><tr>";
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                result += $"<th>{reader.GetName(i)}</th>";
                            }
                            result += "</tr>";

                            while (reader.Read())
                            {
                                result += "<tr>";
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    result += $"<td>{reader.GetValue(i)}</td>";
                                }
                                result += "</tr>";
                            }
                            result += "</table>";
                        }
                        else
                        {
                            result += "<p>No hay datos</p>";
                        }
                    }

                    connection.Close();

                    result += "<br><br><a href='/Category/RecreateDatabase' style='color: red; font-weight: bold;'>⚠️ RECREAR BASE DE DATOS COMPLETA</a>";

                    return Content(result);
                }
            }
            catch (Exception ex)
            {
                return Content($"❌ ERROR: {ex.Message}");
            }
        }

        public IActionResult CheckRealConnection()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("ISCDApplications_DevConn");

                var result = "🔍 VERIFICANDO CONEXIÓN REAL 🔍<br><br>";
                result += $"Cadena de conexión usada:<br><code>{connectionString}</code><br><br>";

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // 1. Verificar servidor y base de datos REAL
                    result += "📊 INFORMACIÓN DEL SERVIDOR:<br>";
                    result += $"   Servidor: {connection.DataSource}<br>";
                    result += $"   Base de datos: {connection.Database}<br>";
                    result += $"   Estado: {connection.State}<br>";
                    result += $"   Versión: {connection.ServerVersion}<br><br>";

                    // 2. Verificar TODAS las bases de datos en este servidor
                    result += "🗄️ BASES DE DATOS EN ESTE SERVIDOR:<br>";
                    var cmdDatabases = new SqlCommand("SELECT name FROM sys.databases ORDER BY name", connection);
                    using (var reader = cmdDatabases.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result += $"   • {reader.GetString(0)}<br>";
                        }
                    }

                    // 3. Verificar datos REALES en Category
                    result += "<br>📋 DATOS REALES EN TABLA 'Category':<br>";
                    var cmdData = new SqlCommand("SELECT Id, Name, DisplayOrder FROM Category ORDER BY Id", connection);
                    using (var reader = cmdData.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            result += "<table border='1'><tr><th>ID</th><th>Name</th><th>DisplayOrder</th></tr>";
                            while (reader.Read())
                            {
                                result += $"<tr><td>{reader.GetInt32(0)}</td><td>{reader.GetString(1)}</td><td>{reader.GetInt32(2)}</td></tr>";
                            }
                            result += "</table>";
                        }
                        else
                        {
                            result += "   (Tabla vacía)<br>";
                        }
                    }

                    connection.Close();
                }

                result += "<br><br>🔧 ACCIONES:<br>";
                result += "   • <a href='/Category/ClearAllData'>Borrar TODOS los datos</a><br>";
                result += "   • <a href='/Category/ShowRealSSMSCode'>Ver código para SSMS</a><br>";

                return Content(result);
            }
            catch (Exception ex)
            {
                return Content($"❌ ERROR: {ex.Message}<br><br>{ex.StackTrace}".Replace("\n", "<br>"));
            }
        }

        public IActionResult ClearAllData()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("ISCDApplications_DevConn");

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // 1. Eliminar todos los datos
                    var cmdDelete = new SqlCommand("DELETE FROM Category", connection);
                    var rowsDeleted = cmdDelete.ExecuteNonQuery();

                    // 2. Resetear identity
                    var cmdReset = new SqlCommand("DBCC CHECKIDENT ('Category', RESEED, 0)", connection);
                    cmdReset.ExecuteNonQuery();

                    // 3. Insertar datos CONFIABLES
                    var cmdInsert = new SqlCommand(@"
                INSERT INTO Category (Name, DisplayOrder) VALUES
                ('Electronics', 1),
                ('Clothing', 2),
                ('Books', 3)
            ", connection);
                    var rowsInserted = cmdInsert.ExecuteNonQuery();

                    connection.Close();

                    return Content(
                        $"✅ DATOS LIMPIADOS Y RECREADOS<br><br>" +
                        $"• {rowsDeleted} registros eliminados<br>" +
                        $"• Identity reseteado<br>" +
                        $"• {rowsInserted} nuevos registros insertados<br><br>" +
                        $"<a href='/Category'>Ver categorías</a><br>" +
                        $"<a href='/Category/CheckRealConnection'>Verificar conexión</a>"
                    );
                }
            }
            catch (Exception ex)
            {
                return Content($"❌ ERROR: {ex.Message}");
            }
        }

        public IActionResult ShowRealSSMSCode()
        {
            var connectionString = _configuration.GetConnectionString("ISCDApplications_DevConn");

            // Extraer información de la cadena de conexión
            var server = "(localdb)\\MSSQLLocalDB";
            var database = "DevWeb";

            if (connectionString.Contains("Server="))
            {
                var start = connectionString.IndexOf("Server=") + 7;
                var end = connectionString.IndexOf(";", start);
                server = connectionString.Substring(start, end - start);
            }

            if (connectionString.Contains("Database="))
            {
                var start = connectionString.IndexOf("Database=") + 9;
                var end = connectionString.IndexOf(";", start);
                database = connectionString.Substring(start, end - start);
            }
            else if (connectionString.Contains("Initial Catalog="))
            {
                var start = connectionString.IndexOf("Initial Catalog=") + 16;
                var end = connectionString.IndexOf(";", start);
                database = connectionString.Substring(start, end - start);
            }

            var code = @$"-- CONÉCTATE A ESTE SERVIDOR en SSMS:
-- Servidor: {server}

-- LUEGO EJECUTA ESTE CÓDIGO:

-- 1. Ver todas las bases de datos en este servidor
SELECT name FROM sys.databases ORDER BY name;

-- 2. Usar la base de datos correcta
USE {database};

-- 3. Ver TODOS los datos (sin TOP)
SELECT * FROM Category ORDER BY Id;

-- 4. Contar registros
SELECT COUNT(*) as TotalRegistros FROM Category;

-- 5. Ver estructura de la tabla
EXEC sp_help 'Category';

-- 6. ELIMINAR datos confusos
/*
DELETE FROM Category;
DBCC CHECKIDENT ('Category', RESEED, 0);

-- Insertar datos claros
INSERT INTO Category (Name, DisplayOrder) VALUES
('Test1', 1),
('Test2', 2),
('Test3', 3);
*/";

            return Content($"<pre>{code}</pre><br>" +
                          "<a href='/Category'>Volver a Category</a>");
        }


        // <param name="Id></param>

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category obj)
        {
            // Validación personalizada
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("Name", "The DisplayOrder cannot exactly match the name.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _db.Categories.Add(obj);
                    _db.SaveChanges();
                    TempData["success"] = "Category created successfully";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Esto te dará el mensaje detallado de SQL
                    var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    ModelState.AddModelError("", "Error detallado: " + innerMessage);
                    return View(obj);
                }
            }

            // ¡IMPORTANTE!: Pasa 'obj' de regreso para que no se pierdan los datos escritos
            return View(obj);
        }

        public IActionResult Edit(int? Id)
        {
            if (Id == null || Id == 0)
            {
                return NotFound();
            }

            Category? categoryFromDb = _db.Categories.Find(Id);
            if (categoryFromDb == null)
            {
                return NotFound();
            }
            return View(categoryFromDb);
        }
        [HttpPost]
        public IActionResult Edit(Category obj)
        {
            if (ModelState.IsValid)
            {
                _db.Categories.Update(obj);
                _db.SaveChanges();
                TempData["success"] = "Category updated successfully"; // Corregido el texto
                return RedirectToAction("Index");
            }
            // Si hay error, regresa el objeto a la vista
            return View(obj);
        }

        public IActionResult Delete(int? Id)
        {
            if (Id == null || Id == 0)
            {
                return NotFound();
            }

            Category? categoryFromDb = _db.Categories.Find(Id);
            if (categoryFromDb == null)
            {
                return NotFound();
            }

            return View(categoryFromDb);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePost(int? Id)
        {
            Category? obj = _db.Categories.Find(Id);
            if (obj == null)
            {
                {
                    return NotFound();
                }
            }

            _db.Categories.Remove(obj);
            _db.SaveChanges();
            TempData["success"] = "Category deleted successfully";
            return RedirectToAction("Index");
        }








    }



}