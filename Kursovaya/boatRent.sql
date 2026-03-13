CREATE DATABASE BoatRent;

USE BoatRent;

CREATE TABLE Roles (
    ID int AUTO_INCREMENT PRIMARY KEY,
    RoleName varchar(50) NOT NULL
);

CREATE TABLE BoatCategories (
    ID int AUTO_INCREMENT PRIMARY KEY,
    CategoryName varchar(100) NOT NULL
);

CREATE TABLE OrderStatuses (
    ID int AUTO_INCREMENT PRIMARY KEY,
    StatusName varchar(50) NOT NULL
);

CREATE TABLE Users (
    ID int AUTO_INCREMENT PRIMARY KEY,
    FullName varchar(100) NOT NULL,
    Login varchar(50) NOT NULL UNIQUE,
    Pass varchar(100) NOT NULL,
    RoleID int NOT NULL,
    FOREIGN KEY (RoleID) REFERENCES Roles(ID)
);

CREATE TABLE Clients (
    ID int AUTO_INCREMENT PRIMARY KEY,
    ClientName varchar(100) NOT NULL,
    Phone varchar(20) NOT NULL,
    Email varchar(100),
    Address varchar(200)
);

CREATE TABLE Boat (
    ID int AUTO_INCREMENT PRIMARY KEY,
    Nam varchar(100) NOT NULL,
    CategoryID int NOT NULL,
    Price decimal(10,2) NOT NULL,
    Description varchar(500),
    FOREIGN KEY (CategoryID) REFERENCES BoatCategories(ID)
);

CREATE TABLE Orders (
    ID int AUTO_INCREMENT PRIMARY KEY,
    OrderDate datetime NOT NULL,
    StartDate date NOT NULL,
    EndDate date NOT NULL,
    TotalPrice decimal(10,2) NOT NULL,
    ClientID int NOT NULL,
    UserID int NOT NULL,
    BoatID int NOT NULL,
    StatusID int NOT NULL,
    FOREIGN KEY (ClientID) REFERENCES Clients(ID),
    FOREIGN KEY (UserID) REFERENCES Users(ID),
    FOREIGN KEY (StatusID) REFERENCES OrderStatuses(ID),
    FOREIGN KEY (BoatID) REFERENCES Boat(ID)
);

CREATE TABLE Reports (
    ID int AUTO_INCREMENT PRIMARY KEY,
    Parameters varchar(500),
    UserID int NOT NULL,
    FOREIGN KEY (UserID) REFERENCES Users(ID)
);

-- Fill Reference Tables
INSERT INTO Roles (ID, RoleName) VALUES 
(1, 'Администратор'), (2, 'Менеджер'), (3, 'Директор');

INSERT INTO BoatCategories (ID, CategoryName) VALUES 
(1,'Яхты'), (2, 'Катеры'), (3, 'Катамараны');

INSERT INTO OrderStatuses (ID, StatusName) VALUES 
(1, 'Новый'), (2, 'Завершен'), (3, 'Отменен');

-- Fill Users Table
INSERT INTO Users (ID, FullName, Login, Pass, RoleID) VALUES 
(1, 'Васильев Александр Васильевич', 'admin', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', 1),
(2, 'Кусакина Мария Валерьевна', 'manager', '1c1c96a3fa0a7b8c4e83c8211c7c417b61864c2d5f2b9e6d6e5a5a5a5a5a5a5a5', 2),
(3, 'Кряжев Дмитрий Анатольевич', 'director', '1c1c96a3fa0a7b8c4e83c8211c7c417b61864c2d5f2b9e6d6e5a5a5a5a5a5a5a5', 3);

INSERT INTO Clients (ID, ClientName, Phone, Email, Address) VALUES 
(1, 'Иванов Александр Сергеевич', '+7-915-123-45-67', 'ivanov.as@mail.ru', 'г. Москва, ул. Центральная, д. 15, кв. 23'),
(2, 'Петрова Екатерина Дмитриевна', '+7-916-234-56-78', 'petrova.ed@gmail.com', 'г. Санкт-Петербург, Невский пр-т, д. 42, кв. 7'),
(3, 'Сидоров Михаил Андреевич', '+7-917-345-67-89', 'sidorov.ma@yandex.ru', 'г. Сочи, ул. Приморская, д. 8, кв. 12'),
(4, 'Козлова Анна Викторовна', '+7-918-456-78-90', 'kozlova.av@mail.ru', 'г. Калининград, ул. Балтийская, д. 33, кв. 5'),
(5, 'Федоров Денис Олегович', '+7-919-567-89-01', 'fedorov.do@gmail.com', 'г. Владивосток, ул. Портовская, д. 17, кв. 9'),
(6, 'Николаева Ольга Игоревна', '+7-920-678-90-12', 'nikolaeva.oi@yandex.ru', 'г. Новосибирск, ул. Ленина, д. 56, кв. 14'),
(7, 'Волков Артем Павлович', '+7-921-789-01-23', 'volkov.ap@mail.ru', 'г. Казань, ул. Кремлевская, д. 22, кв. 3'),
(8, 'Семенова Марина Александровна', '+7-922-890-12-34', 'semenova.ma@gmail.com', 'г. Екатеринбург, ул. Мамина-Сибиряка, д. 45, кв. 11'),
(9, 'Павлов Игорь Владимирович', '+7-923-901-23-45', 'pavlov.iv@yandex.ru', 'г. Ростов-на-Дону, ул. Береговая, д. 9, кв. 6'),
(10, 'Григорьева Татьяна Сергеевна', '+7-924-012-34-56', 'grigoreva.ts@mail.ru', 'г. Краснодар, ул. Красная, д. 78, кв. 18');

-- Исправленная таблица Boat (убраны лишние столбцы)
INSERT INTO Boat (ID, Nam, CategoryID, Price, Description) VALUES 
(1, 'Яхта "Bavaria R40 Coupe"', 1, 16000.00, 'Роскошная яхта с 3 каютами, идеально подходящая для проведения корпоративных мероприятий'),
(2, 'Катер "Thunder"', 2, 14000.00, 'Высокоэффективный скоростной катер для занятий водными видами спорта'),
(3, 'Катамаран "Sunset Cruiser"', 3, 10000.00, 'Устойчивый катамаран для семейных поездок'),
(4, 'Яхта "Royal Pearl"', 1, 18000.00, '80-футовая яхта с роскошными удобствами'),
(5, 'Катер "Lightning"', 2, 12500.00, 'Гоночный катер для опытных водителей');

-- Исправленная таблица Orders (правильные ID для статусов и пользователей)
INSERT INTO Orders (ID, OrderDate, StartDate, EndDate, TotalPrice, ClientID, UserID, BoatID, StatusID) VALUES 
(1, '2024-01-05 09:30:00', '2024-01-10', '2024-01-12', 32000.00, 1, 2, 1, 2),
(2, '2024-01-06 10:15:00', '2024-01-15', '2024-01-17', 28000.00, 2, 2, 2, 2),
(3, '2024-01-07 11:20:00', '2024-01-18', '2024-01-20', 30000.00, 3, 2, 3, 2),
(4, '2024-01-08 14:45:00', '2024-01-22', '2024-01-22', 18000.00, 4, 2, 4, 2),
(5, '2024-01-09 16:30:00', '2024-01-25', '2024-01-27', 37500.00, 5, 2, 5, 3);

-- Установим AUTO_INCREMENT для следующих вставок
ALTER TABLE Orders AUTO_INCREMENT = 6;