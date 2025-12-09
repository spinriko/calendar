# Deployment Guide

## CI/CD Testing Strategy

This project contains two separate test suites that must be run independently:

### 1. C# Tests (90 tests)
Run using `dotnet test`:

```bash
dotnet test --logger "trunit;LogFileName=csharp-test-results.xml"
```

**Output:** `../../pto.track.tests/TestResults/csharp-test-results.xml`

### 2. TypeScript Tests (164 tests)
Run using `npm test`:

```bash
cd ../../pto.track.tests.js
<<<<<<< HEAD:docs/run/DEPLOY.md
./run-headless.sh
```

**Windows:**
```powershell
cd ../../pto.track.tests.js
.\run-headless.ps1
```

**Output:** `../../pto.track.tests.js/test-results.xml`
=======
npm test
```

**Output:** `../../pto.track.tests.js/test-results/jest-junit.xml`
>>>>>>> cdf607bf611f4c99b4a153a0abc255ac735ac5d9:docs/run/DEPLOY.md

### Total Test Coverage
- **254 tests total** (90 C# + 164 TypeScript)
- Both produce JUnit XML format for CI/CD integration

## CI/CD Pipeline Example

### GitHub Actions

```yaml
name: CI

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Run C# tests
      run: dotnet test --no-build --logger "trunit;LogFileName=csharp-test-results.xml"
    
    - name: Run TypeScript tests
      run: |
        cd pto.track.tests.js
        npm ci
        npm test
    
    - name: Publish test results
      uses: dorny/test-reporter@v1
      if: always()
      with:
        name: Test Results
        path: |
          **/csharp-test-results.xml
          **/test-results/jest-junit.xml
        reporter: java-junit
```

### Azure Pipelines

```yaml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: UseDotNet@2
  inputs:
    version: '8.0.x'

- script: dotnet restore
  displayName: 'Restore dependencies'

- script: dotnet build --no-restore
  displayName: 'Build'

- script: dotnet test --no-build --logger "trunit;LogFileName=csharp-test-results.xml"
  displayName: 'Run C# tests'

- script: |
    cd pto.track.tests.js
    ./run-headless.sh
  displayName: 'Run JavaScript tests'

- task: PublishTestResults@2
  inputs:
    testResultsFormat: 'JUnit'
    testResultsFiles: |
      **/csharp-test-results.xml
      **/test-results.xml
    mergeTestResults: true
    testRunTitle: 'All Tests'
  condition: always()
```

### Jenkins

```groovy
pipeline {
    agent any
    
    stages {
        stage('Build') {
            steps {
                sh 'dotnet restore'
                sh 'dotnet build --no-restore'
            }
        }
        
        stage('Test - C#') {
            steps {
                sh 'dotnet test --no-build --logger "trunit;LogFileName=csharp-test-results.xml"'
            }
        }
        
        stage('Test - JavaScript') {
            steps {
                dir('pto.track.tests.js') {
                    sh './run-headless.sh'
                }
            }
        }
    }
    
    post {
        always {
            junit '**/csharp-test-results.xml, **/test-results.xml'
        }
    }
}
```

## Deployment Steps

### 1. Build for Production

```bash
# Publish framework-dependent deployment
dotnet publish pto.track/pto.track.csproj -c Release -o ./publish --no-self-contained

# Or publish self-contained deployment for Linux
dotnet publish pto.track/pto.track.csproj -c Release -o ./publish-standalone --self-contained -r linux-x64
```

### 2. Deploy to Server

Copy the contents of `./publish` or `./publish-standalone` to your server and run:

**Framework-dependent:**
```bash
dotnet pto.track.dll
```

**Self-contained:**
```bash
./pto.track
```

### 3. Environment Configuration

Ensure `appsettings.json` or environment variables are configured for production:
- Database connection strings
- Authentication settings
- Logging configuration

## Pre-Deployment Checklist

- [ ] All 131 tests passing (90 C# + 43 JavaScript)
- [ ] Database migrations applied
- [ ] Configuration files updated for production environment
- [ ] SSL certificates configured
- [ ] Backup of production database completed
- [ ] Rollback plan documented

## Monitoring

After deployment, verify:
- Application starts without errors
- Authentication works correctly
- Database connectivity is functional
- All API endpoints respond as expected
