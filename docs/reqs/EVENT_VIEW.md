# Event View Requirements

## Overview
This document outlines the requirements for improving the User Interface (UI) of the "Create/Edit Absence" modal/view. The goal is to streamline the date and time selection process by reducing clicks and improving visibility.

## Requirements

### 1. Date Selection UI
- **Current Behavior**: Start Date and End Date are hidden behind dropdowns. Clicking the dropdown reveals a calendar picker.
- **New Behavior**: 
  - Remove the dropdown interaction.
  - Display the **Start Date** and **End Date** calendars directly on the form (inline).
  - Position the two calendars **side-by-side** for easy comparison and selection.
  - Ensure clear labeling for "Start Date" and "End Date".

### 2. Time Selection UI
- **Current Behavior**: Start Time and End Time are selected via dropdowns.
- **New Behavior**:
  - Replace dropdowns with dedicated **Time Pickers**.
  - Position the Start Time and End Time pickers **side-by-side**.
  - Ensure clear labeling for "Start Time" and "End Time".

### 3. Default Values
- **Start Time**: Default to **8:00 AM**.
- **End Time**: Default to **5:00 PM**.

### 4. "All Day" Toggle
- Add an **"All Day" checkbox**.
- **Default State**: Checked.
- **Behavior**: When checked, the Time Pickers are hidden or disabled. Unchecking reveals them.

### 5. Dynamic Duration Display
- Display a calculated total (e.g., "Total: 3 Days" or "Total: 4 Hours").
- **Behavior**: Updates instantly as dates or times change.
- **Benefit**: Provides immediate feedback to the user.

### 6. Smart Date Logic
- **Auto-set End Date**: If the user selects a Start Date, the End Date should default to the same day (if not already set or if it was previously invalid).
- **Validation**: Prevent selecting an End Date that is *before* the Start Date (e.g., disable those days in the End Date calendar).

### 7. Responsive Layout
- **Desktop**: Side-by-side layout for calendars and time pickers.
- **Mobile/Narrow Screens**: Stack calendars and time pickers vertically to ensure the modal fits the screen.

### 8. Time Picker Granularity
- **Step**: Use **15 or 30 minute** increments for the time picker.
- **Benefit**: Faster selection than free-text input for standard PTO requests.

## Bug list
- duration not updating correctly in any day/week/month view if time component introduced.  Day duration seems to function ok
