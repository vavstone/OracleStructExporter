drop table {префикс}PROCESS;
drop table {префикс}CONNWORKLOG;
drop table {префикс}COMMITS;
drop table {префикс}COMMITDETAILS;

drop sequence {префикс}PROCESS_SEQ;
drop sequence {префикс}CONNWORKLOG_SEQ;
drop sequence {префикс}COMMITS_SEQ;
drop sequence {префикс}COMMITDETAILS_SEQ;

create table {префикс}PROCESS
(
  id                     NUMBER not null,
  connections_to_process NUMBER not null,
  start_time             DATE not null,
  end_time               DATE,
  processobjcountplan    NUMBER,
  processobjcountfact    NUMBER,
  errorscount            NUMBER
);
alter table {префикс}PROCESS  add constraint {префикс}PROCESS_PK primary key (ID);



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
);
create index IDX_{префикс}_CNWL_DB on {префикс}CONNWORKLOG (DBID);
create index IDX_{префикс}_CNWL_EL on {префикс}CONNWORKLOG (EVENTLEVEL);
create index IDX_{префикс}_CNWL_ERR on {префикс}CONNWORKLOG (ERRORSCOUNT);
create index IDX_{префикс}_CNWL_PI on {префикс}CONNWORKLOG (PROCESS_ID);
create index IDX_{префикс}_CNWL_ST on {префикс}CONNWORKLOG (STAGE);
create index IDX_{префикс}_CNWL_UN on {префикс}CONNWORKLOG (USERNAME);
create index IDX_{префикс}_CNWL_ET on {префикс}CONNWORKLOG (EVENTTIME);
alter table {префикс}CONNWORKLOG  add constraint {префикс}CONNWORKLOG_PK primary key (ID);
  

create table {префикс}COMMITS
(
  id                 	NUMBER not null,
  process_id         	NUMBER not null,
  dbid               	VARCHAR2(50) not null,
  username           	VARCHAR2(50) not null,
  commit_common_date 	DATE not null,
  is_initial		 	NUMBER(1) not null,
  
  dbl_add_cnt			NUMBER not null,
  dbl_add_size			NUMBER not null,
  dbl_upd_cnt			NUMBER not null,
  dbl_upd_size			NUMBER not null,
  dbl_del_cnt			NUMBER not null,
  dbl_del_prev_size		NUMBER not null,
  
  dbj_add_cnt			NUMBER not null,
  dbj_add_size			NUMBER not null,
  dbj_upd_cnt			NUMBER not null,
  dbj_upd_size			NUMBER not null,
  dbj_del_cnt			NUMBER not null,
  dbj_del_prev_size		NUMBER not null,
  
  fnc_add_cnt			NUMBER not null,
  fnc_add_size			NUMBER not null,
  fnc_upd_cnt			NUMBER not null,
  fnc_upd_size			NUMBER not null,
  fnc_del_cnt			NUMBER not null,
  fnc_del_prev_size		NUMBER not null,
  
  pkg_add_cnt			NUMBER not null,
  pkg_add_size			NUMBER not null,
  pkg_upd_cnt			NUMBER not null,
  pkg_upd_size			NUMBER not null,
  pkg_del_cnt			NUMBER not null,
  pkg_del_prev_size		NUMBER not null,
  
  prc_add_cnt			NUMBER not null,
  prc_add_size			NUMBER not null,
  prc_upd_cnt			NUMBER not null,
  prc_upd_size			NUMBER not null,
  prc_del_cnt			NUMBER not null,
  prc_del_prev_size		NUMBER not null,
  
  scj_add_cnt			NUMBER not null,
  scj_add_size			NUMBER not null,
  scj_upd_cnt			NUMBER not null,
  scj_upd_size			NUMBER not null,
  scj_del_cnt			NUMBER not null,
  scj_del_prev_size		NUMBER not null,
  
  seq_add_cnt			NUMBER not null,
  seq_add_size			NUMBER not null,
  seq_upd_cnt			NUMBER not null,
  seq_upd_size			NUMBER not null,
  seq_del_cnt			NUMBER not null,
  seq_del_prev_size		NUMBER not null,
  
  syn_add_cnt			NUMBER not null,
  syn_add_size			NUMBER not null,
  syn_upd_cnt			NUMBER not null,
  syn_upd_size			NUMBER not null,
  syn_del_cnt			NUMBER not null,
  syn_del_prev_size		NUMBER not null,
  
  tab_add_cnt			NUMBER not null,
  tab_add_size			NUMBER not null,
  tab_upd_cnt			NUMBER not null,
  tab_upd_size			NUMBER not null,
  tab_del_cnt			NUMBER not null,
  tab_del_prev_size		NUMBER not null,
  
  trg_add_cnt			NUMBER not null,
  trg_add_size			NUMBER not null,
  trg_upd_cnt			NUMBER not null,
  trg_upd_size			NUMBER not null,
  trg_del_cnt			NUMBER not null,
  trg_del_prev_size		NUMBER not null,
  
  tps_add_cnt			NUMBER not null,
  tps_add_size			NUMBER not null,
  tps_upd_cnt			NUMBER not null,
  tps_upd_size			NUMBER not null,
  tps_del_cnt			NUMBER not null,
  tps_del_prev_size		NUMBER not null,
  
  viw_add_cnt			NUMBER not null,
  viw_add_size			NUMBER not null,
  viw_upd_cnt			NUMBER not null,
  viw_upd_size			NUMBER not null,
  viw_del_cnt			NUMBER not null,
  viw_del_prev_size		NUMBER not null,
  
  all_add_cnt			NUMBER not null,
  all_add_size			NUMBER not null,
  all_upd_cnt			NUMBER not null,
  all_upd_size			NUMBER not null,
  all_del_cnt			NUMBER not null,
  all_del_prev_size		NUMBER not null
);
create index IDX_{префикс}_CMT_DB on {префикс}COMMITS (DBID);
create index IDX_{префикс}_CMT_UN on {префикс}COMMITS (USERNAME);
create index IDX_{префикс}_CMT_PI on {префикс}COMMITS (PROCESS_ID);
create index IDX_{префикс}_CMT_CD on {префикс}COMMITS (COMMIT_COMMON_DATE);
create index IDX_{префикс}_CMT_II on {префикс}COMMITS (IS_INITIAL);

alter table {префикс}COMMITS  add constraint {префикс}CMT_PK primary key (ID);    
  
create table {префикс}COMMITDETAILS
(
  id                 	NUMBER not null,
  process_id         	NUMBER not null,
  dbid               	VARCHAR2(50) not null,
  username           	VARCHAR2(50) not null,
  commit_common_date 	DATE not null,
  commit_cur_file_time  DATE not null,
  is_initial		 	NUMBER(1) not null,
  commit_oper		 	NUMBER not null,
  commit_file		 	VARCHAR2(200) not null,
  commit_file_size		NUMBER not null,
  obj_type			 	NUMBER not null
);
create index IDX_{префикс}_CMD_DB on {префикс}COMMITDETAILS (DBID);
create index IDX_{префикс}_CMD_UN on {префикс}COMMITDETAILS (USERNAME);
create index IDX_{префикс}_CMD_PI on {префикс}COMMITDETAILS (PROCESS_ID);
create index IDX_{префикс}_CMD_CD on {префикс}COMMITDETAILS (COMMIT_COMMON_DATE);
create index IDX_{префикс}_CMD_II on {префикс}COMMITDETAILS (IS_INITIAL);
create index IDX_{префикс}_CMD_CO on {префикс}COMMITDETAILS (COMMIT_OPER);
create index IDX_{префикс}_CMD_OT on {префикс}COMMITDETAILS (OBJ_TYPE);
alter table {префикс}COMMITDETAILS  add constraint {префикс}CMD_PK primary key (ID);  


create sequence {префикс}PROCESS_SEQ
minvalue 1
maxvalue 999999999999999999999999999
start with 1
increment by 1
cache 20;

create sequence {префикс}CONNWORKLOG_SEQ
minvalue 1
maxvalue 999999999999999999999999999
start with 1
increment by 1
cache 20;

create sequence {префикс}COMMITS_SEQ
minvalue 1
maxvalue 999999999999999999999999999
start with 1
increment by 1
cache 20;

create sequence {префикс}COMMITDETAILS_SEQ
minvalue 1
maxvalue 999999999999999999999999999
start with 1
increment by 1
cache 20;