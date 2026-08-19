import sqlite3

# Initialize the database

# Author: Agaesh Kumar A/L N Senturvasan
# Description: This file contains the database class that handles the database connection and table creation.   
class Database:

    def __init__(self, db_name):
        self.conn = sqlite3.connect(db_name)
        self.cursor = self.conn.cursor()
        self.create_tables()

    def create_auth_tables(self):
        # Create roles/permissions tables for authentication and authorization
        print("Creating authentication tables...")
        
        # Create roles table
        self.cursor.execute('''
            CREATE TABLE IF NOT EXISTS roles(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                role_name TEXT(50) NOT NULL UNIQUE,
                description TEXT(200),
                is_active BOOLEAN NOT NULL DEFAULT 1,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP
            )
        ''')

        self.conn.commit()
        print("Roles table created successfully.")
        
        # Create permissions table
        self.cursor.execute('''
            CREATE TABLE IF NOT EXISTS permissions(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                permission_name TEXT(100) NOT NULL UNIQUE,
                resource TEXT(100) NOT NULL,
                action TEXT(50) NOT NULL,
                description TEXT(200),
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                CHECK (action IN ('CREATE', 'READ', 'UPDATE', 'DELETE', 'EXECUTE'))
            )
        ''')

        self.conn.commit()
        print("Permissions table created successfully.")
        
        # Create role_permissions junction table
        self.cursor.execute('''
            CREATE TABLE IF NOT EXISTS role_permissions(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                role_id INTEGER NOT NULL,
                permission_id INTEGER NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE,
                FOREIGN KEY (permission_id) REFERENCES permissions(id) ON DELETE CASCADE,
                UNIQUE(role_id, permission_id)
            )
        ''')

        self.conn.commit()
        print("Role_Permissions table created successfully.")
        
        # Create user_roles junction table (extends the existing users table)
        self.cursor.execute('''
            CREATE TABLE IF NOT EXISTS user_roles(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                role_id INTEGER NOT NULL,
                assigned_by INTEGER,
                assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                expires_at TIMESTAMP,
                is_active BOOLEAN DEFAULT 1,
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
                FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE,
                FOREIGN KEY (assigned_by) REFERENCES users(id),
                UNIQUE(user_id, role_id)
            )
        ''')

        self.conn.commit()
        print("User_Roles table created successfully.")
        
        # Create refresh_tokens table for JWT authentication
        self.cursor.execute('''
            CREATE TABLE IF NOT EXISTS refresh_tokens(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                token TEXT NOT NULL UNIQUE,
                expires_at TIMESTAMP NOT NULL,
                is_revoked BOOLEAN DEFAULT 0,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                user_agent TEXT,
                ip_address TEXT,
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
            )
        ''')

        self.conn.commit()
        print("Refresh_Tokens table created successfully.")
        
        # Create password_reset_tokens table
        self.cursor.execute('''
            CREATE TABLE IF NOT EXISTS password_reset_tokens(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                token TEXT NOT NULL UNIQUE,
                expires_at TIMESTAMP NOT NULL,
                is_used BOOLEAN DEFAULT 0,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
            )
        ''')

        self.conn.commit()
        print("Password_Reset_Tokens table created successfully.")
        
        # Create login_audit table for tracking login attempts
        self.cursor.execute('''
            CREATE TABLE IF NOT EXISTS login_audit(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER,
                username TEXT,
                login_successful BOOLEAN NOT NULL,
                login_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                ip_address TEXT,
                user_agent TEXT,
                failure_reason TEXT,
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL
            )
        ''')

        self.conn.commit()
        print("Login_Audit table created successfully.")
        
        # Create indexes for performance
        self.cursor.execute('''
            CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_id ON refresh_tokens(user_id)
        ''')
        self.cursor.execute('''
            CREATE INDEX IF NOT EXISTS idx_refresh_tokens_token ON refresh_tokens(token)
        ''')
        self.cursor.execute('''
            CREATE INDEX IF NOT EXISTS idx_password_reset_user_id ON password_reset_tokens(user_id)
        ''')
        self.cursor.execute('''
            CREATE INDEX IF NOT EXISTS idx_login_audit_user_id ON login_audit(user_id)
        ''')
        self.cursor.execute('''
            CREATE INDEX IF NOT EXISTS idx_login_audit_username ON login_audit(username)
        ''')
        self.cursor.execute('''
            CREATE INDEX IF NOT EXISTS idx_user_roles_user_id ON user_roles(user_id)
        ''')
        
        self.conn.commit()
        print("Authentication indexes created successfully.")
        
        # Insert default roles
        print("Inserting default roles...")
        default_roles = [
            ('SUPER_ADMIN', 'System administrator with all permissions'),
            ('ADMIN', 'Administrator with most permissions'),
            ('MANAGER', 'Manager with operational permissions'),
            ('STAFF', 'Regular staff with basic permissions'),
            ('SUPPLIER_STAFF', 'Supplier staff with limited permissions')
        ]
        
        for role_name, description in default_roles:
            self.cursor.execute('''
                INSERT OR IGNORE INTO roles (role_name, description)
                VALUES (?, ?)
            ''', (role_name, description))
        
        self.conn.commit()
        print("Default roles inserted successfully.")
        
        # Insert default permissions
        print("Inserting default permissions...")
        default_permissions = [
            ('user_create', 'users', 'CREATE', 'Create new users'),
            ('user_read', 'users', 'READ', 'View user information'),
            ('user_update', 'users', 'UPDATE', 'Update user information'),
            ('user_delete', 'users', 'DELETE', 'Delete users'),
            ('role_create', 'roles', 'CREATE', 'Create new roles'),
            ('role_read', 'roles', 'READ', 'View role information'),
            ('role_update', 'roles', 'UPDATE', 'Update role information'),
            ('role_delete', 'roles', 'DELETE', 'Delete roles'),
            ('product_create', 'products', 'CREATE', 'Create new products'),
            ('product_read', 'products', 'READ', 'View product information'),
            ('product_update', 'products', 'UPDATE', 'Update product information'),
            ('product_delete', 'products', 'DELETE', 'Delete products'),
            ('order_create', 'orders', 'CREATE', 'Create new orders'),
            ('order_read', 'orders', 'READ', 'View order information'),
            ('order_update', 'orders', 'UPDATE', 'Update order information'),
            ('order_delete', 'orders', 'DELETE', 'Delete orders'),
            ('supplier_create', 'suppliers', 'CREATE', 'Create new suppliers'),
            ('supplier_read', 'suppliers', 'READ', 'View supplier information'),
            ('supplier_update', 'suppliers', 'UPDATE', 'Update supplier information'),
            ('supplier_delete', 'suppliers', 'DELETE', 'Delete suppliers'),
            ('report_view', 'reports', 'READ', 'View reports'),
            ('report_export', 'reports', 'EXECUTE', 'Export reports'),
            ('accounting_view', 'accounting', 'READ', 'View accounting information'),
            ('accounting_update', 'accounting', 'UPDATE', 'Update accounting information'),
            ('purchase_create', 'purchase', 'CREATE', 'Create purchase orders'),
            ('purchase_read', 'purchase', 'READ', 'View purchase information'),
            ('purchase_update', 'purchase', 'UPDATE', 'Update purchase information'),
            ('purchase_approve', 'purchase', 'EXECUTE', 'Approve purchase orders'),
            ('inventory_create', 'inventory', 'CREATE', 'Create inventory entries'),
            ('inventory_read', 'inventory', 'READ', 'View inventory information'),
            ('inventory_update', 'inventory', 'UPDATE', 'Update inventory information'),
            ('inventory_adjust', 'inventory', 'EXECUTE', 'Adjust inventory levels')
        ]
        
        for perm_name, resource, action, description in default_permissions:
            self.cursor.execute('''
                INSERT OR IGNORE INTO permissions (permission_name, resource, action, description)
                VALUES (?, ?, ?, ?)
            ''', (perm_name, resource, action, description))
        
        self.conn.commit()
        print("Default permissions inserted successfully.")

    def create_tables(self):
        
        # Create accounts table 
        print("Creating accounts table...")
        self.cursor.execute('''
            CREATE TABLE IF NOT EXISTS accounts(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                account_code TEXT(20) NOT NULL UNIQUE,
                account_name TEXT(20) NOT NULL,
                account_type TEXT NOT NULL CHECK(account_type IN ('ASSET', 'LIABILITY', 'EQUITY', 'REVENUE', 'EXPENSE')),
                parent_id INTEGER,
                normal_balance TEXT NOT NULL DEFAULT 'DEBIT' CHECK(normal_balance IN ('DEBIT', 'CREDIT')),
                description TEXT(100),
                is_active BOOLEAN NOT NULL DEFAULT 1,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP,
                FOREIGN KEY (parent_id) REFERENCES accounts(id)
            )
        ''')
        self.conn.commit()

        # write to screen that the accounts table has been created
        print("Account Table has been created successfully.")

        # Create users table
        self.cursor.execute('''
           CREATE TABLE IF NOT EXISTS users (
           id INTEGER PRIMARY KEY AUTOINCREMENT,
           username TEXT NOT NULL UNIQUE,
           email TEXT NOT NULL UNIQUE,
           password TEXT NOT NULL,
           full_name TEXT NOT NULL,
           user_type TEXT NOT NULL 
           CHECK (user_type IN ('ADMIN', 'MANAGER', 'STAFF', 'SUPPLIER_STAFF')),
           is_active BOOLEAN DEFAULT 1,
           created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
           last_login TIMESTAMP,
           supplier_id INTEGER REFERENCES suppliers(id) ON DELETE SET NULL
        -- NULL for restaurant staff, filled for supplier staff
);
        ''')

         # write to screen that the users table has been created
        print("Users table created successfully.")        
        self.conn.commit()
                
       # Create Suppliers
        self.cursor.execute('''
            CREATE TABLE IF NOT EXISTS suppliers (
                id INTEGER PRIMARY KEY AUTOINCREMENT,

                -- Supplier identification
                company_name TEXT(255) NOT NULL,
                business_registration_no TEXT(50),
                tin_no TEXT(50),

                -- Contact
                email_address TEXT(255),
                website_url TEXT(255),
                contact_no1 TEXT(30),
                contact_no2 TEXT(30),
                contact_no3 TEXT(30),

                -- Address
                address_line1 TEXT(255),
                address_line2 TEXT(255),
                city TEXT(100),
                state TEXT(100),
                postal_code TEXT(20),
                country_code TEXT(3) NOT NULL DEFAULT 'MY',

                -- Default transaction currency
                currency_code TEXT(3) NOT NULL DEFAULT 'MYR',

                -- Payment terms
                credit_limit DECIMAL(15,2) NOT NULL DEFAULT 0,
                credit_days INTEGER NOT NULL DEFAULT 0,

                -- Accounting
                account_payable_id INTEGER,

                -- Supplier status
                status TEXT NOT NULL DEFAULT 'ACTIVE'
                    CHECK (status IN ('ACTIVE', 'INACTIVE', 'BLOCKED')),

                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )
        ''')

        self.conn.commit()
        print("Suppliers table created successfully.")


        # Create raw materials 

        self.cursor.execute('''CREATE TABLE IF NOT EXISTS raw_materials (
            id INTEGER PRIMARY KEY AUTOINCREMENT,   
            name TEXT NOT NULL,
            description TEXT,
            price REAL NOT NULL CHECK (price >= 0),
            quantity REAL NOT NULL DEFAULT 0 CHECK (quantity >= 0),
            image_location TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )''')

        print("Raw materials table created successfully.")
        self.conn.commit()

        # Create products table
        self.cursor.execute('''CREATE TABLE IF NOT EXISTS products (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            price REAL NOT NULL CHECK (price >= 0),
            image_location TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )''')

        print("Products table created successfully.")
        self.conn.commit()

        #Create Product_Raw Material table 
        self.cursor.execute(''' CREATE TABLE product_raw_materials (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            product_id INTEGER NOT NULL,
            raw_material_id INTEGER NOT NULL,
            quantity REAL NOT NULL CHECK (quantity > 0),
            unit TEXT NOT NULL,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (product_id) REFERENCES products(id),
            FOREIGN KEY (raw_material_id) REFERENCES raw_materials(id)
        )''')

        print("Product_Raw_Materials table created successfully.")

        # CREATE order table
        self.cursor.execute('''CREATE TABLE IF NOT EXISTS orders(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER NOT NULL,
            total_price REAL NOT NULL CHECK (total_price >= 0),
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (user_id) REFERENCES users(id)
        )''')

        self.conn.commit()
        # write to screen that the orders table has been created
        print("Orders table created successfully.")     

        # Create Orders item Table 
        self.cursor.execute('''CREATE TABLE IF NOT EXISTS order_items(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            order_id INTEGER NOT NULL,
            product_id INTEGER NOT NULL,
            quantity REAL NOT NULL CHECK (quantity > 0),
            tax_rate REAL CHECK (tax_rate * 100 = CAST(tax_rate * 100 AS INTEGER)),
            tax_amount REAL NOT NULL,
            total_price REAL NOT NULL CHECK (total_price >= 0),
            subtotal_price REAL NOT NULL CHECK (subtotal_price >= 0),
            subtotal_price_with_tax REAL NOT NULL CHECK (subtotal_price_with_tax >= 0),
            order_status TEXT CHECK(order_status IN ('pending', 'shipped', 'delivered', 'cancelled')),            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (order_id) REFERENCES orders(id),
            FOREIGN KEY (product_id) REFERENCES products(id)
        )''')

        self.conn.commit()
        # write to screen that the orders table has been created    
        print("Orders table created successfully.")

        # create reviews table 
        print("Creating reviews table...")
        self.cursor.execute('''CREATE TABLE IF NOT EXISTS reviews(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER NOT NULL,
            product_id INTEGER NOT NULL,
            rating INTEGER NOT NULL CHECK (rating >= 1 AND rating <= 5),
            comment TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (user_id) REFERENCES users(id),         
            FOREIGN KEY (product_id) REFERENCES products(id)
        )''')

        self.conn.commit()
        print("Reviews table created successfully.")

        # Create Tax Table 

        self.cursor.execute('''CREATE TABLE IF NOT EXISTS tax(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            tax_name TEXT NOT NULL,
            tax_rate REAL NOT NULL CHECK (tax_rate * 100 = CAST(tax_rate * 100 AS INTEGER)),
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )''')

        # Create the tax regime table.
        # Requirements:
        # - Only one active tax regime can exist at a time.
        # - For example, if tax_regime_id = 1 is currently active,
        #   another active tax regime cannot be created.
        # - Multiple inactive tax regimes are allowed for historical records.
        # - Therefore, the database must prevent multiple active tax regimes
        #   from existing simultaneously.

        self.cursor.execute("""
            CREATE TABLE IF NOT EXISTS tax_regime (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                tax_regime_id INTEGER NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT 1,
                effective_date DATE NOT NULL,
                effective_end_date DATE,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (tax_regime_id)
                    REFERENCES tax(id)
            )
        """)

        self.conn.commit()
        # write to screen that the tax regime table has been created
        print("Tax regime table created successfully.")

        # CREATE unique index on tax_regime_id and is_active to ensure that only one active tax regime can exist at a time
        self.cursor.execute('''
            CREATE UNIQUE INDEX IF NOT EXISTS
            idx_tax_regime_active ON tax_regime (tax_regime_id, is_active)
            WHERE is_active = 1
        ''')

        # write to screen that the unique index has been created
        self.conn.commit()
        print("Created Unique index on tax_regime_id and is_active to ensure that only one active tax regime can exist at a time.")

        self.cursor.execute('''CREATE TABLE IF NOT EXISTS acc_payments(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER NOT NULL,
            payment_reference TEXT NOT NULL UNIQUE,
            payment_type TEXT NOT NULL,
            order_id INTEGER NOT NULL,
            currency_code TEXT NOT NULL DEFAULT 'MYR',
            payment_amount REAL NOT NULL DEFAULT 0 CHECK (payment_amount >= 0),
            payment_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            payment_status TEXT NOT NULL DEFAULT 'pending',
            CHECK (payment_status IN ('pending', 'completed', 'failed')),
            FOREIGN KEY (user_id) REFERENCES users(id),
            FOREIGN KEY (order_id) REFERENCES orders(id)
        )
        ''')

        self.conn.commit()
        #Write to screen that the acc_payments table has been created
        print("Acc_payments table created successfully.")

        print("Creating Purchase Related Tables")

        # CREATE doc table to store PO, INV AND GRN

        self.cursor.execute('''
            CREATE TABLE IF NOT EXISTS documents (
                id INTEGER PRIMARY KEY AUTOINCREMENT,

                doc_number TEXT NOT NULL UNIQUE,
                doc_type TEXT NOT NULL,

                issued_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

                doc_status TEXT NOT NULL DEFAULT 'draft',

                created_by INTEGER NOT NULL,

                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

                FOREIGN KEY (created_by) REFERENCES users(id),

                CHECK (
                    doc_type IN (
                        'PO',
                        'GRN',
                        'PI',
                        'SI',
                        'CN'
                    )
                ),

                CHECK (
                    doc_status IN (
                        'draft',
                        'issued',
                        'completed',
                        'cancelled'
                    )
                )
            )
        ''')

        self.conn.commit()
        print("Documents table created successfully.")
        print("Creating Purchase Tables")

        # CREATE purchase_order table
        self.cursor.execute('''
            CREATE TABLE IF NOT EXISTS purchase_orders (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            doc_id INTEGER NOT NULL,
            supplier_id INTEGER NOT NULL,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            remarks TEXT,
            payment_terms TEXT,
            currency_code TEXT NOT NULL DEFAULT 'MYR',
            delivery_address TEXT,
            expected_delivery_date TIMESTAMP,
            FOREIGN KEY (doc_id) REFERENCES documents(id),
            FOREIGN KEY (supplier_id) REFERENCES suppliers(id)
            )
'''     )
        
        self.conn.commit()
        print("Purchase Orders table created successfully.")


        # CREATE purchase_order_items table
        self.cursor.execute('''
            CREATE TABLE IF NOT EXISTS purchase_order_items (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
          
            purchase_order_id INTEGER NOT NULL,
            item_id INTEGER NOT NULL,
            
            quantity REAL NOT NULL DEFAULT 0 CHECK (quantity >= 0),
            unit TEXT NOT NULL,

            price REAL NOT NULL DEFAULT 0 CHECK (price >= 0),

            tax_rate REAL NOT NULL DEFAULT 0
                CHECK (
                    tax_rate >= 0
                    AND tax_rate <= 100
                    AND tax_rate * 100 =
                        CAST(tax_rate * 100 AS INTEGER)
                ),

            tax_amount REAL NOT NULL DEFAULT 0
                CHECK (tax_amount >= 0),

            total_price REAL NOT NULL DEFAULT 0
                CHECK (total_price >= 0),

            FOREIGN KEY (purchase_order_id)
                REFERENCES purchase_orders(id),

            FOREIGN KEY (item_id)
                REFERENCES raw_materials(id)
            )
'''     )

        self.conn.commit()

        print("Purchase Order Items table created successfully.")

        # Create Supplier Invoice Table
        print("Creating Supplier Invoice Table")

        self.cursor.execute('''
            CREATE TABLE IF NOT EXISTS supplier_invoices(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            doc_id INTEGER NOT NULL,
            purchase_order_id INTEGER NOT NULL,
            supplier_id INTEGER NOT NULL,
            status TEXT NOT NULL,
            billing_address TEXT(255),
            notes TEXT,
            approval_status CHECK (approval_status IN ('PENDING', 'APPROVED', 'REJECTED')) NOT NULL DEFAULT 'pending',
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
        ''')
        self.conn.commit()
        print("Supplier Invoice table created successfully")
        # CREATE supplier_invoice_items table
        print("Creating Supplier Invoice Items Table")
        self.cursor.execute('''
            CREATE TABLE IF NOT EXISTS supplier_invoice_items (
            id INTEGER PRIMARY KEY AUTOINCREMENT,

            supplier_invoice_id INTEGER NOT NULL,
            purchase_order_item_id INTEGER,

            item_id INTEGER NOT NULL,

            quantity REAL NOT NULL CHECK (quantity > 0),
            unit TEXT NOT NULL,

            price REAL NOT NULL CHECK (price >= 0),

            tax_rate REAL NOT NULL DEFAULT 0
            CHECK (tax_rate >= 0 AND tax_rate <= 100),

            tax_amount REAL NOT NULL DEFAULT 0
            CHECK (tax_amount >= 0),

            total_price REAL NOT NULL DEFAULT 0
            CHECK (total_price >= 0),

            FOREIGN KEY (supplier_invoice_id)
            REFERENCES supplier_invoices(id),

            FOREIGN KEY (purchase_order_item_id)
            REFERENCES purchase_order_items(id),

            FOREIGN KEY (item_id)
            REFERENCES raw_materials(id)
            )
'''     )
        
        self.conn.commit()
        print("Supplier Invoice Items table created successfully.") 


        print("Creating Customer table")

        self.cursor.create('''
         
           CREATE TABLE Customers(
            id INTEGER AUTO_INCREMENT,
            firstname TEXT(30)
            lastname TEXT(30)
           )
        ''')