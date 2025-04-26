CREATE TABLE authors
(
   au_id          varchar(11)

         CHECK (au_id like '[0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9][0-9]')

         CONSTRAINT UPKCL_auidind PRIMARY KEY CLUSTERED,

   au_lname       varchar(40)       NOT NULL,
   au_fname       varchar(20)       NOT NULL,

   phone          char(12)          NOT NULL

         DEFAULT ('UNKNOWN'),

   address        varchar(40)           NULL,
   city           varchar(20)           NULL,
   state          char(2)               NULL,

   zip            char(5)               NULL

         CHECK (zip like '[0-9][0-9][0-9][0-9][0-9]'),

   contract       bit               NOT NULL
)

GO