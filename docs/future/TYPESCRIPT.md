# Converting JavaScript Tests to TypeScript

## Overview

Converting the JavaScript tests to TypeScript is very feasible and would provide significant benefits, particularly for code quality and getting working coverage reports.

## What Needs to be Converted

1. All test files in `pto.track.tests.js/tests/*.test.js` → `*.test.ts`
2. Helper files like `calendar-functions.js` → `calendar-functions.ts`
3. Mock file `mock-daypilot.js` → `mock-daypilot.ts`
4. Jest config `jest.config.js` → `jest.config.ts` (optional)

## Required Steps

### 1. Install TypeScript Dependencies

```bash
npm install --save-dev typescript @types/jest @types/node ts-jest
```

### 2. Configure TypeScript

- Create `tsconfig.json` for type checking and compiler options
- Update `jest.config.js` to use `ts-jest` preset instead of experimental VM modules

Example `tsconfig.json`:
```json
{
  "compilerOptions": {
    "target": "ES2020",
    "module": "commonjs",
    "lib": ["ES2020", "DOM"],
    "strict": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "forceConsistentCasingInFileNames": true,
    "moduleResolution": "node",
    "resolveJsonModule": true,
    "types": ["jest", "node"]
  },
  "include": ["tests/**/*", "*.ts"],
  "exclude": ["node_modules"]
}
```

Example `jest.config.js` update:
```javascript
export default {
  preset: 'ts-jest',
  testEnvironment: 'jsdom',
  testMatch: ['**/tests/**/*.test.ts'],
  collectCoverageFrom: ['tests/**/*.ts', '!tests/**/*.test.ts'],
  coverageDirectory: 'coverage',
  reporters: [
    'default',
    ['jest-junit', {
      outputDirectory: './test-results',
      outputName: 'jest-junit.xml',
    }]
  ]
};
```

### 3. Type the DayPilot Mock

Define interfaces for `DayPilot.Calendar`, `DayPilot.Event`, etc.

Example type definitions:
```typescript
declare namespace DayPilot {
  interface Calendar {
    events: { list: Event[] };
    update(): void;
    // ... other methods
  }
  
  interface Event {
    id(): string;
    data: {
      id: string;
      text: string;
      start: Date | string;
      end: Date | string;
      resource?: string;
      [key: string]: any;
    };
  }
  
  // ... other interfaces
}
```

### 4. Convert Test Files

- Add type annotations to test parameters
- Type the mock data structures
- Add return types to helper functions

Example conversion:
```typescript
// Before (JS)
function createMockEvent(id, text, start, end) {
  return { id, text, start, end };
}

// After (TS)
interface EventData {
  id: string;
  text: string;
  start: string;
  end: string;
}

function createMockEvent(id: string, text: string, start: string, end: string): EventData {
  return { id, text, start, end };
}
```

## Benefits

- **Catch type errors before runtime** - No more undefined property errors at test time
- **Better IDE autocomplete and intellisense** - IntelliSense will know exactly what properties/methods are available
- **Self-documenting code** - Types serve as inline documentation
- **Easier refactoring** - Rename/refactor with confidence that types will catch issues
- **Coverage would likely work!** - ts-jest doesn't use experimental VM modules, so Jest coverage should work properly

## Potential Issues

1. **DayPilot typing** - DayPilot doesn't have official TypeScript types, so custom type definitions will be needed
2. **Learning curve** - If the team isn't familiar with TypeScript, there's a learning investment
3. **Build complexity** - Additional transpilation step, though ts-jest handles this automatically
4. **Migration effort** - Estimated 2-3 hours to convert all files and set up properly

## Estimated Timeline

- **Setup & configuration**: 30 minutes
- **Type definitions for DayPilot**: 1 hour
- **Convert test files**: 1-1.5 hours
- **Testing & fixes**: 30 minutes

**Total: 2-3 hours**

## Additional Considerations

### Coverage Benefits

The biggest win would be getting working coverage reports. Since `ts-jest` doesn't have the ESM experimental module limitation that's currently blocking Jest coverage, converting to TypeScript would likely solve the coverage problem entirely.

### Type Safety

TypeScript would catch many potential bugs:
- Typos in property names
- Incorrect function arguments
- Missing required properties
- Incorrect return types

### Future Maintenance

Once converted, maintaining the tests becomes easier:
- Adding new tests follows clear patterns
- Refactoring is safer with compiler checks
- New team members can understand code faster with explicit types

## Next Steps (When Ready)

1. Create a new branch: `feature/typescript-tests`
2. Install dependencies
3. Create `tsconfig.json` and update `jest.config.js`
4. Create DayPilot type definitions
5. Convert files one at a time, testing as you go
6. Verify all 164 tests still pass
7. Verify coverage now works
8. Commit and merge
