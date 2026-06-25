USE cybersecurity_bot;

CREATE TABLE tasks (
    task_id INT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(50) NOT NULL,
    title NVARCHAR(100) NOT NULL,
    description NVARCHAR(MAX),
    reminder_date DATETIME,
    is_completed BIT DEFAULT 0,
    created_date DATETIME DEFAULT GETDATE()
);

-- Create index for faster queries
CREATE INDEX idx_username ON tasks(username);

-- Test: Insert a sample record
INSERT INTO tasks (username, title, description) 
VALUES ('testuser', 'Test Task', 'This is a test task');