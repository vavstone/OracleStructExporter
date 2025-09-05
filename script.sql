create table {префикс}CONNWORKLOG
(
  id                 NUMBER not null,
  process_id         NUMBER not null,
  dbid               VARCHAR2(50) not null,
  username           VARCHAR2(50) not null,
  stage              VARCHAR2(50) not null,
  eventlevel         VARCHAR2(50) not null,
  eventid            VARCHAR2(36) not null,
  eventtime          DATE not null,
  message            VARCHAR2(1000),
  typeobjcountplan   NUMBER,
  currentnum         NUMBER,
  schemaobjcountfact NUMBER,
  typeobjcountfact   NUMBER,
  metaobjcountfact   NUMBER,
  errorscount        NUMBER,
  schemaobjcountplan NUMBER,
  objtype            VARCHAR2(50),
  objname            VARCHAR2(200)
)
;
create index IDX_{префикс}_CNWL_DB on {префикс}CONNWORKLOG (DBID);
create index IDX_{префикс}_CNWL_EL on {префикс}CONNWORKLOG (EVENTLEVEL);
create index IDX_{префикс}_CNWL_ERR on {префикс}CONNWORKLOG (ERRORSCOUNT);
create index IDX_{префикс}_CNWL_PI on {префикс}CONNWORKLOG (PROCESS_ID);
create index IDX_{префикс}_CNWL_ST on {префикс}CONNWORKLOG (STAGE);
create index IDX_{префикс}_CNWL_UN on {префикс}CONNWORKLOG (USERNAME);
create index IDX_{префикс}_CNWL_ET on {префикс}CONNWORKLOG (EVENTTIME);

alter table {префикс}CONNWORKLOG
  add constraint {префикс}CONNWORKLOG_PK primary key (ID);

create sequence {префикс}CONNWORKLOG_SEQ
minvalue 1
maxvalue 999999999999999999999999999
start with 1
increment by 1
cache 20;

create table {префикс}PROCESS
(
  id                     NUMBER not null,
  connections_to_process NUMBER not null,
  start_time             DATE not null,
  end_time               DATE,
  processobjcountplan    NUMBER,
  processobjcountfact    NUMBER,
  errorscount            NUMBER
)
;
alter table {префикс}PROCESS
  add constraint {префикс}PROCESS_PK primary key (ID);

create sequence {префикс}PROCESS_SEQ
minvalue 1
maxvalue 999999999999999999999999999
start with 1
increment by 1
cache 20;