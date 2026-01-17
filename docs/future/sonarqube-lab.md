# SonarQube Lab Preparation Checklist  
_Preparing to deploy a fully automated SonarQube VM using Terraform + Ansible, including cold-standby and recovery steps._

---

# 1. Lab Environment Prerequisites

## 1.1 Access & Credentials
- [ ] Azure subscription with permissions to create resource groups, VMs, disks, and networking.
- [ ] Service principal or user credentials for Terraform authentication.
- [ ] SSH keypair (Linux VM) or admin credentials (Windows VM, if applicable).
- [ ] Local workstation with:
  - [ ] Terraform installed
  - [ ] Ansible installed (if using Ansible)
  - [ ] Azure CLI installed

## 1.2 Networking Requirements
- [ ] Identify subnet for the VM.
- [ ] Confirm outbound internet access for package installation (or prepare offline packages).
- [ ] Define inbound rules (typically only port 9000 for SonarQube UI).
- [ ] Determine whether the VM will be public-facing or private-only.

## 1.3 Storage & Database Decisions
- [ ] Decide whether PostgreSQL will run:
  - [ ] On the VM (cheapest, simplest)
  - [ ] As Azure Database for PostgreSQL Flexible Server (more enterprise-friendly)
- [ ] Determine disk sizes:
  - [ ] Premium SSD for OS disk
  - [ ] Standard SSD for data disk (if DB is local)

---

# 2. Terraform Preparation

## 2.1 Terraform Structure
- [ ] Create Terraform project folder with:
  - [ ] `main.tf`
  - [ ] `variables.tf`
  - [ ] `outputs.tf`
  - [ ] `providers.tf`
- [ ] Define:
  - [ ] Resource group
  - [ ] Virtual network + subnet
  - [ ] Network security group
  - [ ] Public IP (optional)
  - [ ] NIC
  - [ ] VM definition (size, OS, disks)
  - [ ] Data disk (if hosting PostgreSQL locally)
  - [ ] Cloud-init or Custom Script Extension (if not using Ansible)

## 2.2 Variables to Parameterize
- [ ] VM size (D2s v5 or D4s v5 recommended)
- [ ] Admin username
- [ ] SSH key or password
- [ ] SonarQube version
- [ ] Java distribution (Temurin recommended)
- [ ] Storage sizes
- [ ] Network CIDR ranges

## 2.3 Terraform Validation
- [ ] `terraform init`
- [ ] `terraform validate`
- [ ] `terraform plan` (ensure no surprises)
- [ ] Confirm cost estimates

---

# 3. Ansible Preparation (if using Ansible)

## 3.1 Ansible Project Structure
- [ ] Create:
  - [ ] `inventory` file
  - [ ] `site.yml` playbook
  - [ ] `roles/sonarqube/` directory
  - [ ] `roles/postgresql/` directory (if DB is local)

## 3.2 SonarQube Role Tasks
- [ ] Install Java (Temurin or Zulu)
- [ ] Create `sonarqube` user
- [ ] Download SonarQube distribution
- [ ] Extract to `/opt/sonarqube`
- [ ] Configure `sonar.properties`
- [ ] Configure systemd service
- [ ] Apply sysctl tuning for Elasticsearch
- [ ] Open firewall ports (9000)

## 3.3 PostgreSQL Role Tasks (if local)
- [ ] Install PostgreSQL
- [ ] Create database and user
- [ ] Configure authentication
- [ ] Tune memory settings
- [ ] Enable and start service

## 3.4 Validation
- [ ] Run playbook against lab VM
- [ ] Confirm SonarQube starts cleanly
- [ ] Confirm UI loads
- [ ] Confirm DB connectivity

---

# 4. Cold Standby Preparation

## 4.1 Standby VM Definition
- [ ] Create a second VM definition in Terraform:
  - [ ] Same size
  - [ ] Same OS
  - [ ] Same disk layout
  - [ ] **Do not start the VM** (keep deallocated)
- [ ] Tag it clearly as `sonarqube-standby`.

## 4.2 Database Backup Strategy
- [ ] Nightly `pg_dump` (if DB is local)
- [ ] Or Azure PostgreSQL automated backups (if using Flexible Server)
- [ ] Store backups in:
  - [ ] Azure Storage Account
  - [ ] Or internal file share

## 4.3 VM Snapshot Strategy
- [ ] Nightly snapshot of primary VM OS disk
- [ ] Nightly snapshot of data disk (if DB is local)
- [ ] Retention policy (e.g., 7–14 days)

---

# 5. Outage Recovery Procedure (Cold Standby)

## 5.1 Trigger Condition
- [ ] Primary VM is unreachable or corrupted
- [ ] SonarQube service fails and cannot be restored quickly
- [ ] Disk corruption or OS failure

## 5.2 Recovery Steps
1. **Start the standby VM**  
   - [ ] Power on the deallocated standby VM.

2. **Restore the database**  
   - If DB is local:
     - [ ] Attach latest data disk snapshot  
     - [ ] Or restore from `pg_dump`  
   - If DB is Azure PostgreSQL:
     - [ ] Restore to a new server or point-in-time restore

3. **Update DNS**  
   - [ ] Point `sonar.company.com` to the standby VM IP  
   - [ ] TTL should be low (e.g., 5 minutes)

4. **Validate service**  
   - [ ] Confirm SonarQube UI loads  
   - [ ] Confirm projects and history are intact  
   - [ ] Run a test scan from CI

5. **Post-recovery cleanup**  
   - [ ] Investigate primary VM failure  
   - [ ] Rebuild or replace primary VM using Terraform  
   - [ ] Re-establish standby VM as cold backup

---

# 6. Lab Validation Checklist

## 6.1 Functional Validation
- [ ] SonarQube UI loads
- [ ] Login works
- [ ] Sample project scans successfully
- [ ] Quality Gate triggers correctly

## 6.2 Performance Validation
- [ ] VM handles typical scan load
- [ ] Elasticsearch tuning validated
- [ ] Memory usage stable

## 6.3 Resilience Validation
- [ ] Snapshot restore tested
- [ ] DB restore tested
- [ ] Standby VM activation tested
- [ ] DNS failover tested

---

# 7. Deliverables for Infra Team

- [ ] Terraform project folder (validated)
- [ ] Ansible playbooks (or cloud-init scripts)
- [ ] Architecture diagram
- [ ] Cost estimate
- [ ] Backup/restore guide
- [ ] Cold standby activation guide
- [ ] Lab validation report

---

# Summary

This checklist prepares a complete, reproducible lab environment for deploying SonarQube using Terraform and Ansible, including a cold-standby resilience model and a clear outage recovery procedure. It ensures the infra team receives a turnkey, low-friction deployment package requiring minimal cloud expertise.
