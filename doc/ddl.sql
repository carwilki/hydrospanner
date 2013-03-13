CREATE TABLE messages
(
    sequence bigint identity(1,1) not null,
    wire_id uniqueidentifier null,
    type_name varchar(1024) not null,
    payload varbinary(max) not null,
    headers varbinary(max) null,
    CONSTRAINT PK_messages PRIMARY KEY CLUSTERED (sequence)
);
CREATE TABLE checkpoints
(
    dispatch bigint not null,
    CONSTRAINT PK_checkpoints PRIMARY KEY CLUSTERED (dispatch)
);