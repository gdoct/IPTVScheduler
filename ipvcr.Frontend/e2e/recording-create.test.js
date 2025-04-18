/**
 * Recording creation end-to-end test
 * Tests the functionality to create a new recording
 */

/**
 * Helper function for login
 * @param {import('puppeteer').Page} page - Puppeteer page instance
 * @param {Object} config - Test configuration
 */
async function performLogin(page, config) {
  // Navigate to login page
  const loginUrl = `${config.baseUrl}/login`;
  console.log(`Navigating to ${loginUrl}`);
  await page.goto(loginUrl, {
    waitUntil: 'networkidle2',
    timeout: config.timeout
  });
  
  // Wait for the login form to be available
  await page.waitForSelector('form', { timeout: config.timeout });
  
  // Fill out the login form
  await page.type('input[type="text"]', 'admin');
  await page.type('input[type="password"]', 'ipvcr');
  
  // Submit the form by clicking the login button
  console.log('Submitting login form');
  await Promise.all([
    page.click('button[type="submit"]'),
    page.waitForNavigation({ waitUntil: 'networkidle2', timeout: config.timeout })
  ]).catch(error => {
    throw new Error(`Failed to submit login form or navigation: ${error.message}`);
  });
  
  // Verify successful login by checking redirect to recordings page
  try {
    await page.waitForFunction(
      () => window.location.pathname.includes('/recordings'),
      { timeout: config.timeout }
    );
    console.log('Successfully logged in - redirected to recordings page');
  } catch (error) {
    const errorElement = await page.$('.alert-danger');
    if (errorElement) {
      const errorText = await page.evaluate(el => el.textContent, errorElement);
      throw new Error(`Login failed with error: ${errorText}`);
    } else {
      throw new Error('Login verification failed - not redirected to recordings page');
    }
  }
}

/**
 * Generate a random recording name
 * @returns {string} A random recording name with timestamp
 */
function generateRandomRecordingName() {
  return `Test Recording ${new Date().toISOString().replace(/[:.]/g, '-')}`;
}

/**
 * Helper function to wait in older versions of Puppeteer that don't have waitForTimeout
 * @param {import('puppeteer').Page} page - Puppeteer page instance 
 * @param {number} ms - Time to wait in milliseconds
 * @returns {Promise<void>}
 */
async function waitFor(page, ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

/**
 * Main test function that will be executed by the test runner
 * @param {import('puppeteer').Browser} browser - Puppeteer browser instance
 * @param {Object} config - Test configuration
 */
async function run(browser, config) {
  // Create a new page
  const page = await browser.newPage();
  
  // Set viewport size
  await page.setViewport({ width: 1280, height: 800 });
  
  // Step 1: Login first
  await performLogin(page, config);
  
  // Take a screenshot after login
  await page.screenshot({ 
    path: `${config.screenshotsDir}/recording-create-after-login.png`,
    fullPage: true 
  });
  
  // Step 2: Find and click "Add Recording" button using multiple possible selectors
  console.log('Looking for "Add Recording" button');
  
  // Try different selectors for the "Add Recording" button
  const addButtonSelectors = [
    '[data-testid="add-recording-btn"]',
    'button:contains("Add")',
    'button:contains("New")',
    'button:contains("Recording")',
    'a:contains("Add")',
    'a:contains("New")',
    'a.btn',
    'button.btn-primary'
  ];
  
  console.log('Taking screenshot of recordings page to help debug');
  await page.screenshot({ 
    path: `${config.screenshotsDir}/recordings-page.png`,
    fullPage: true 
  });
  
  console.log('Looking for elements on page that might be the Add Recording button');
  
  // Get all buttons and links on the page for debugging
  const pageButtons = await page.evaluate(() => {
    const buttons = Array.from(document.querySelectorAll('button, a.btn, a[href*="new"], a[href*="add"]'));
    return buttons.map(btn => ({
      tag: btn.tagName,
      text: btn.textContent.trim(),
      classes: btn.className,
      href: btn.tagName === 'A' ? btn.href : null,
      id: btn.id,
      dataAttrs: Array.from(btn.attributes)
        .filter(attr => attr.name.startsWith('data-'))
        .map(attr => `${attr.name}="${attr.value}"`)
        .join(', ')
    }));
  });
  
  console.log('Found potential buttons:', JSON.stringify(pageButtons, null, 2));
  
  let buttonFound = false;
  
  // First try the specific selectors
  for (const selector of addButtonSelectors) {
    try {
      const buttonExists = await page.$(selector);
      if (buttonExists) {
        console.log(`Found "Add Recording" button with selector: ${selector}`);
        await Promise.all([
          page.click(selector),
          page.waitForNavigation({ waitUntil: 'networkidle2', timeout: config.timeout })
        ]).catch(error => {
          console.log(`Navigation after clicking Add Recording button failed: ${error.message}`);
          // We'll try another method if this fails
        });
        buttonFound = true;
        break;
      }
    } catch (error) {
      console.log(`Selector ${selector} not found: ${error.message}`);
    }
  }
  
  // If button not found by selector, try using text content
  if (!buttonFound) {
    console.log('No button found with specific selectors, trying text content approach');
    
    const buttonClicked = await page.evaluate(() => {
      // Try to find button by text content first
      const buttonTexts = ['Add Recording', 'New Recording', 'Add', 'New', 'Create Recording'];
      
      for (const text of buttonTexts) {
        // Look for buttons and links
        const elements = Array.from(document.querySelectorAll('button, a'));
        const button = elements.find(el => 
          el.textContent.trim().toLowerCase().includes(text.toLowerCase())
        );
        
        if (button) {
          button.click();
          return true;
        }
      }
      
      // If still not found, look for anything that's likely to be an add button
      const addElements = Array.from(document.querySelectorAll('.btn-add, .add-btn, .btn-new, .btn-primary, a[href*="new"], a[href*="add"]'));
      if (addElements.length > 0) {
        addElements[0].click();
        return true;
      }
      
      return false;
    });
    
    if (buttonClicked) {
      console.log('Found and clicked a button that might be the Add Recording button');
      // Wait for navigation
      try {
        await page.waitForNavigation({ waitUntil: 'networkidle2', timeout: config.timeout });
        buttonFound = true;
      } catch (error) {
        console.log('No navigation detected after clicking potential Add button');
      }
    }
  }
  
  if (!buttonFound) {
    throw new Error('Could not find any button to add a new recording');
  }
  
  // Verify we're on the new recording page or a page for adding recordings
  try {
    await page.waitForFunction(
      () => window.location.pathname.includes('/new') || 
             window.location.pathname.includes('/add') || 
             window.location.pathname.includes('/create'),
      { timeout: config.timeout }
    );
    console.log('Successfully navigated to new recording page');
  } catch (error) {
    console.log('Warning: URL does not contain /new, /add, or /create, but continuing anyway');
    // Take a screenshot to see where we are
    await page.screenshot({ 
      path: `${config.screenshotsDir}/possible-new-recording-page.png`,
      fullPage: true 
    });
  }
  
  // Take a screenshot of the new recording form
  await page.screenshot({ 
    path: `${config.screenshotsDir}/recording-create-form.png`,
    fullPage: true 
  });
  
  // Step 3: Fill in the recording form
  const recordingName = generateRandomRecordingName();
  console.log(`Filling form with recording name: ${recordingName}`);
  
  // Wait for the form to be fully loaded and take a screenshot to help debug
  console.log('Waiting for form elements to be available...');
  await waitFor(page, 1000); // Allow time for form to render
  await page.screenshot({ 
    path: `${config.screenshotsDir}/recording-create-before-input.png`,
    fullPage: true 
  });
  
  // Try to find the name input with multiple possible selectors
  console.log('Looking for recording name input field');
  try {
    // Try different selectors in order of preference
    const nameInputSelectors = [
      '[data-testid="recording-name-input"]',
      'input[name="name"]',
      'input[placeholder*="name" i]',
      'input[id*="name" i]',
      'form input[type="text"]:first-of-type'
    ];
    
    let nameInput = null;
    for (const selector of nameInputSelectors) {
      nameInput = await page.$(selector);
      if (nameInput) {
        console.log(`Found name input with selector: ${selector}`);
        await page.type(selector, recordingName);
        break;
      }
    }
    
    if (!nameInput) {
      console.log('No matching input field found using selectors');
      
      // Get all visible input elements as a fallback
      console.log('Attempting to find all visible text inputs');
      const inputFields = await page.evaluate(() => {
        const allInputs = Array.from(document.querySelectorAll('input[type="text"]'));
        return allInputs
          .filter(input => {
            const style = window.getComputedStyle(input);
            return style.display !== 'none' && style.visibility !== 'hidden';
          })
          .map((input, index) => ({
            index,
            id: input.id,
            name: input.name,
            placeholder: input.placeholder,
            label: input.labels && input.labels[0] ? input.labels[0].textContent : null
          }));
      });
      
      console.log('Found inputs:', JSON.stringify(inputFields));
      
      // Use the first visible input as a last resort
      if (inputFields.length > 0) {
        console.log('Using first visible input field as fallback');
        await page.evaluate((name) => {
          const inputs = Array.from(document.querySelectorAll('input[type="text"]'));
          const visibleInput = inputs.find(input => {
            const style = window.getComputedStyle(input);
            return style.display !== 'none' && style.visibility !== 'hidden';
          });
          if (visibleInput) visibleInput.value = name;
        }, recordingName);
      } else {
        throw new Error('Could not find any suitable input field for recording name');
      }
    }
  } catch (error) {
    console.error('Error filling in recording name:', error);
    await page.screenshot({ 
      path: `${config.screenshotsDir}/recording-create-error-name-input.png`,
      fullPage: true 
    });
    throw error;
  }
  
  // Start typing in the channel autocomplete with improved error handling
  try {
    console.log('Looking for channel search input field');
    const channelInputSelectors = [
      '[data-testid="channel-search-input"]',
      'input[name*="channel" i]',
      'input[placeholder*="channel" i]',
      'input[aria-label*="channel" i]',
      'form input[type="text"]:nth-of-type(2)'
    ];
    
    let channelInput = null;
    for (const selector of channelInputSelectors) {
      channelInput = await page.$(selector);
      if (channelInput) {
        console.log(`Found channel input with selector: ${selector}`);
        console.log('Typing "VIA" in channel field');
        await page.type(selector, 'VIA');
        break;
      }
    }
    
    if (!channelInput) {
      throw new Error('Could not find channel search input');
    }
  } catch (error) {
    console.error('Error finding or filling channel input:', error);
    await page.screenshot({ 
      path: `${config.screenshotsDir}/recording-create-error-channel-input.png`,
      fullPage: true 
    });
    throw error;
  }

  // Wait for the dropdown with potential multiple updates
  console.log('Waiting for channel dropdown to stabilize...');

  // Add initial delay to allow dropdown time to appear and start updating
  await waitFor(page, 1500);

  // Try multiple selectors for the dropdown
  const dropdownSelectors = [
    '[data-testid="channel-dropdown"]',
    '.dropdown-menu',
    '.autocomplete-results',
    'ul[role="listbox"]',
    'div[role="listbox"]'
  ];

  // Try to find the dropdown using different selectors
  let dropdownFound = false;
  for (const selector of dropdownSelectors) {
    try {
      console.log(`Looking for dropdown with selector: ${selector}`);
      await page.waitForSelector(selector, { 
        timeout: 5000,
        visible: true
      });
      console.log(`Found dropdown with selector: ${selector}`);
      dropdownFound = true;
      
      // Take screenshot of dropdown
      await page.screenshot({ 
        path: `${config.screenshotsDir}/channel-dropdown-found.png`,
        fullPage: true 
      });
      
      // Try to select an item from the dropdown
      const foundItem = await page.evaluate((selector) => {
        const dropdown = document.querySelector(selector);
        if (!dropdown) return false;
        
        // Find all potential clickable items in the dropdown
        const items = dropdown.querySelectorAll('li, div[role="option"], .dropdown-item, a');
        
        // Try to find an item containing "VIA"
        for (const item of items) {
          if (item.textContent && item.textContent.includes('VIA')) {
            item.click();
            return true;
          }
        }
        return false;
      }, selector);
      
      if (foundItem) {
        console.log('Successfully clicked on channel option containing "VIA"');
        break;
      }
    } catch (error) {
      console.log(`Selector ${selector} not found: ${error.message}`);
    }
  }

  // If dropdown wasn't found using selectors, try a more generic approach
  if (!dropdownFound) {
    console.log('No dropdown found by selectors, trying generic approach');
    
    // Take a screenshot for debugging
    await page.screenshot({ 
      path: `${config.screenshotsDir}/channel-dropdown-debug.png`,
      fullPage: true 
    });
    
    // Try clicking any visible option that contains "VIA"
    const foundItem = await page.evaluate(() => {
      const allElements = Array.from(document.querySelectorAll('li, div[role="option"], .dropdown-item, a'));
      const viaItem = allElements.find(el => el.textContent && 
                                      el.textContent.includes('VIA') && 
                                      window.getComputedStyle(el).display !== 'none');
      if (viaItem) {
        viaItem.click();
        return true;
      }
      return false;
    });
    
    if (foundItem) {
      console.log('Found and clicked an element containing "VIA" using generic search');
    } else {
      console.log('Warning: Could not find any VIA option, continuing test anyway');
    }
  }
  
  // Take a screenshot after filling the form
  await page.screenshot({ 
    path: `${config.screenshotsDir}/recording-create-filled-form.png`,
    fullPage: true 
  });
  
  // Step 4: Try to save the recording with multiple possible selectors
  console.log('Looking for save recording button');
  const saveButtonSelectors = [
    '[data-testid="save-recording-btn"]',
    'button[type="submit"]',
    'input[type="submit"]',
    'button:has-text("Save")',
    'button:has-text("Create")',
    'button.btn-primary'
  ];
  
  let saveButtonFound = false;
  for (const selector of saveButtonSelectors) {
    try {
      const buttonExists = await page.$(selector);
      if (buttonExists) {
        console.log(`Found save button with selector: ${selector}`);
        console.log('Saving the recording');
        await Promise.all([
          page.click(selector),
          page.waitForNavigation({ waitUntil: 'networkidle2', timeout: config.timeout })
        ]).catch(error => {
          console.log(`Navigation after clicking save button failed: ${error.message}`);
          // Continue anyway, we'll check the current URL later
        });
        saveButtonFound = true;
        break;
      }
    } catch (error) {
      console.log(`Selector ${selector} not found or error clicking: ${error.message}`);
    }
  }
  
  if (!saveButtonFound) {
    console.log('Could not find a save button using selectors, taking a screenshot to debug');
    await page.screenshot({ 
      path: `${config.screenshotsDir}/recording-create-save-button-debug.png`,
      fullPage: true 
    });
    
    // Try generic approach to find any button that might save the form
    const clickedButton = await page.evaluate(() => {
      const buttons = Array.from(document.querySelectorAll('button'));
      // Look for likely save buttons
      const saveButton = buttons.find(btn => {
        const text = btn.textContent.toLowerCase();
        return text.includes('save') || text.includes('create') || text.includes('submit');
      });
      if (saveButton) {
        saveButton.click();
        return true;
      }
      return false;
    });
    
    if (clickedButton) {
      console.log('Clicked a button that might save the form');
      // Wait for potential navigation
      try {
        await page.waitForNavigation({ timeout: config.timeout });
      } catch (error) {
        console.log('No navigation detected after clicking button');
      }
    } else {
      throw new Error('Could not find any button to save the recording');
    }
  }
  
  // Try to verify we're back on the recordings list page
  try {
    await page.waitForFunction(
      () => window.location.pathname === '/recordings' || window.location.pathname.includes('/recordings'),
      { timeout: config.timeout }
    );
    console.log('Successfully returned to recordings list page');
  } catch (error) {
    console.log('Warning: Not redirected to recordings page after saving, taking a screenshot');
    await page.screenshot({ 
      path: `${config.screenshotsDir}/recording-create-after-save.png`,
      fullPage: true 
    });
    // Continue with the test anyway
  }
  
  // Take a screenshot of the recordings list
  await page.screenshot({ 
    path: `${config.screenshotsDir}/recording-create-list-after.png`,
    fullPage: true 
  });
  
  // Step 5: Verify the new recording is in the list
  console.log(`Verifying recording "${recordingName}" appears in the list`);
  
  // Search for the recording name in the list
  const recordingExists = await page.evaluate((name) => {
    // Look for the name in any element on the page that might be part of the list
    const allText = document.body.textContent || '';
    return allText.includes(name);
  }, recordingName);
  
  if (!recordingExists) {
    throw new Error(`New recording "${recordingName}" not found in the recordings list`);
  }
  
  console.log('Recording creation test passed! New recording was successfully created and is visible in the list.');
}

module.exports = { run };