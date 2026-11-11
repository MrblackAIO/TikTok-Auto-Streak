// TikTok Message Automation Script
// This script is injected into the TikTok WebView to automate messaging

(function () {
    var userName = '[UserName]';
    var message = '[Message]';
    var isGroup = '[IsGroup]' === 'true';
    var found = false;
    var chatIndex = 0;
    var chatItems = [];
    var maxScrollAttempts = 50;
    var scrollAttempts = 0;
    var checkedUsernames = {};
    var newItemsFound = 0;

    var log = function (msg) {
        if (typeof StreakApp == 'undefined') {
            console.log(msg);
            return;
        }
        StreakApp.log(msg);
    };

    // ── Environment & Diagnostics ────────────────────────────────────────────

    var dumpEnvironment = function () {
        log('=== FULL ENVIRONMENT DUMP ===');
        log('[ENV] User Agent: ' + navigator.userAgent);
        log('[ENV] Platform: ' + navigator.platform);
        log('[ENV] Page URL: ' + window.location.href);
        log('[ENV] Page Title: ' + document.title);
        log('[ENV] Document Ready: ' + document.readyState);
        log('[ENV] Total DOM elements: ' + document.querySelectorAll('*').length);
        log('[ENV] Viewport: ' + window.innerWidth + 'x' + window.innerHeight);
        log('[ENV] Screen: ' + screen.width + 'x' + screen.height);

        log('[DOM] <html>: ' + (document.documentElement ? 'YES' : 'NO'));
        log('[DOM] <body>: ' + (document.body ? 'YES' : 'NO'));
        log('[DOM] Elements with id: ' + document.querySelectorAll('[id]').length);
        log('[DOM] Elements with class: ' + document.querySelectorAll('[class]').length);
        log('[DOM] Elements with data-e2e: ' + document.querySelectorAll('[data-e2e]').length);

        // TikTok-specific structure checks
        log('[TIKTOK] #app: ' + (document.getElementById('app') ? 'YES' : 'NO'));
        log('[TIKTOK] [id*="app"]: ' + document.querySelectorAll('[id*="app"]').length);
        log('[TIKTOK] [class*="messages"]: ' + document.querySelectorAll('[class*="messages"]').length);
        log('[TIKTOK] [class*="Messages"]: ' + document.querySelectorAll('[class*="Messages"]').length);
        log('[TIKTOK] [class*="chat"]: ' + document.querySelectorAll('[class*="chat"]').length);
        log('[TIKTOK] [class*="Chat"]: ' + document.querySelectorAll('[class*="Chat"]').length);
        log('[TIKTOK] [class*="conversation"]: ' + document.querySelectorAll('[class*="conversation"]').length);
        log('[TIKTOK] [class*="Conversation"]: ' + document.querySelectorAll('[class*="Conversation"]').length);
        log('[TIKTOK] [contenteditable]: ' + document.querySelectorAll('[contenteditable]').length);

        // Framework detection
        log('[FRAMEWORK] window.React: ' + (typeof window.React !== 'undefined' ? 'YES' : 'NO'));
        log('[FRAMEWORK] __REACT_DEVTOOLS_GLOBAL_HOOK__: ' + (typeof window.__REACT_DEVTOOLS_GLOBAL_HOOK__ !== 'undefined' ? 'YES' : 'NO'));
        log('[FRAMEWORK] __NEXT_DATA__: ' + (typeof window.__NEXT_DATA__ !== 'undefined' ? 'YES' : 'NO'));

        log('=== END ENVIRONMENT DUMP ===');
    };

    var dumpDataE2eValues = function () {
        var allE2e = document.querySelectorAll('[data-e2e]');
        var uniqueVals = {};
        for (var i = 0; i < allE2e.length; i++) {
            var val = allE2e[i].getAttribute('data-e2e');
            if (val) uniqueVals[val] = true;
        }
        var keys = Object.keys(uniqueVals);
        log('[DATA-E2E] Total elements: ' + allE2e.length + ', Unique values: ' + keys.length);

        var chunk = [];
        for (var j = 0; j < keys.length; j++) {
            chunk.push(keys[j]);
            if (chunk.length >= 15 || j === keys.length - 1) {
                log('[DATA-E2E] ' + chunk.join(', '));
                chunk = [];
            }
        }
    };

    var searchUsernameInDom = function () {
        log('[USERNAME-SEARCH] Hunting for: ' + userName);
        var xpath = "//*[contains(text(), '" + userName + "')]";
        var result = document.evaluate(xpath, document, null, XPathResult.ANY_TYPE, null);
        var node = result.iterateNext();
        var foundNodes = 0;

        while (node && foundNodes < 5) {
            foundNodes++;
            var current = node;
            var path = [];
            var foundE2e = null;

            for (var k = 0; k < 8 && current && current !== document.body; k++) {
                path.unshift(current.tagName);
                if (current.hasAttribute && current.hasAttribute('data-e2e')) {
                    foundE2e = current.getAttribute('data-e2e');
                    break;
                }
                current = current.parentNode;
            }

            if (foundE2e) {
                log('[USERNAME-SEARCH] Found inside data-e2e="' + foundE2e + '" (tags: ' + path.join('>') + ')');
            } else {
                log('[USERNAME-SEARCH] Found text, no data-e2e parent. Tags: ' + path.join('>'));
            }

            node = result.iterateNext();
        }

        if (foundNodes === 0) {
            log('[USERNAME-SEARCH] CRITICAL: "' + userName + '" not found anywhere in DOM!');
        }
    };

    var dumpFullDiagnostics = function () {
        dumpEnvironment();
        log('=== PAGE DIAGNOSTICS START ===');
        dumpDataE2eValues();
        searchUsernameInDom();

        // Log visible body text length to detect empty/blocked pages
        var bodyText = (document.body && document.body.innerText) || '';
        log('[PAGE] Body text length: ' + bodyText.length + ' chars');
        if (bodyText.length < 200) {
            log('[PAGE] Body text (short page): ' + bodyText.substring(0, 200));
        }

        // Check for common TikTok error/block indicators
        var errorIndicators = ['captcha', 'verify', 'blocked', 'suspended', 'error', 'login'];
        for (var i = 0; i < errorIndicators.length; i++) {
            if (bodyText.toLowerCase().indexOf(errorIndicators[i]) !== -1) {
                log('[PAGE] WARNING: Body contains "' + errorIndicators[i] + '"');
            }
        }

        log('=== PAGE DIAGNOSTICS END ===');
    };

    // ── Chat Item Discovery ──────────────────────────────────────────────────

    var findChatItems = function () {
        // Primary: current TikTok DM selectors
        var items = document.querySelectorAll("[data-e2e*='dm-new-conversation-item'], [data-e2e*='conversation-item'], [data-e2e*='conversation']");
        if (items.length > 0) {
            log('[FIND] Found ' + items.length + ' items via: conversation selectors');
            return items;
        }

        // Fallback selectors — TikTok periodically renames data-e2e values.
        // Keep these specific. Broad class/role matches click the wrong row.
        var fallbacks = [
            "a[href*='/messages/']",
            "[data-e2e*='chat-list-item']",
            "[data-e2e*='chat-item']",
            "[class*='ConversationItem']",
            "[class*='conversation-item']"
        ];
        for (var i = 0; i < fallbacks.length; i++) {
            try {
                items = document.querySelectorAll(fallbacks[i]);
                if (items.length > 0) {
                    log('[FIND] Found ' + items.length + ' items via fallback: ' + fallbacks[i]);
                    return items;
                }
            } catch (e) { }
        }

        log('[FIND] WARNING: No chat items found with any selector');
        return [];
    };

    var findChatListContainer = function () {
        if (chatItems.length > 0) {
            var parent = chatItems[0].parentElement;
            while (parent && parent !== document.body) {
                if (parent.scrollHeight > parent.clientHeight + 10) {
                    return parent;
                }
                parent = parent.parentElement;
            }
        }
        var candidates = document.querySelectorAll('[class*="ChatList"], [class*="chatList"], [class*="conversation-list"]');
        for (var i = 0; i < candidates.length; i++) {
            if (candidates[i].scrollHeight > candidates[i].clientHeight + 10) {
                return candidates[i];
            }
        }
        return null;
    };

    // ── Scroll & Retry ───────────────────────────────────────────────────────

    var scrollAndRetry = function () {
        scrollAttempts++;
        if (scrollAttempts > maxScrollAttempts) {
            log('[SCROLL] Max scroll attempts reached (' + maxScrollAttempts + ')');
            reportError('User not found in chat list');
            return;
        }
        var container = findChatListContainer();
        if (!container) {
            log('[SCROLL] No scrollable chat container found');
            reportError('User not found in chat list');
            return;
        }
        var prevScrollTop = container.scrollTop;
        var prevCount = chatItems.length;
        // Scroll by one viewport height, not to the bottom — TikTok's virtualized
        // list recycles DOM nodes and jumping to scrollHeight skips conversations.
        container.scrollTop += container.clientHeight * 3;
        log('[SCROLL] Scrolling chat list (attempt ' + scrollAttempts + '/' + maxScrollAttempts + ', scrolling by ' + (container.clientHeight * 3) + 'px)...');
        setTimeout(function () {
            chatItems = findChatItems();
            log('[SCROLL] After scroll: ' + chatItems.length + ' items, scrollTop: ' + container.scrollTop + ' (was ' + prevScrollTop + ')');

            if (container.scrollTop <= prevScrollTop) {
                log('[SCROLL] Scroll did not move — end of list');
                reportError('User not found in chat list');
                return;
            }

            // After scrolling, clicking items near the top scrolls the sidebar
            // back up, undoing our scroll. Only check items from the lower portion
            // of the list where new content from the scroll appears.
            // Use max(0, prevCount - 5) to catch a few overlapping items for safety.
            var startFrom = Math.max(0, prevCount - 15);
            newItemsFound = 0;
            log('[SCROLL] Scanning from item ' + (startFrom + 1) + '/' + chatItems.length + ' (known: ' + Object.keys(checkedUsernames).length + ' users)');
            chatIndex = startFrom;
            checkNextChat();
        }, 2000);
    };

    // ── Chat List Exclusion Helper ──────────────────────────────────────────

    var isInsideChatList = function (element) {
        if (!element) return false;
        var container = findChatListContainer();
        if (container && container.contains(element)) return true;
        return !!(
            element.closest('[data-e2e*="conversation-item"]') ||
            element.closest('[data-e2e*="chat-list-item"]') ||
            element.closest('[class*="ConversationItem"]') ||
            element.closest('[class*="conversation-item"]') ||
            element.closest('[class*="ChatItem"]') ||
            element.closest('[class*="chat-item"]') ||
            element.closest('nav') ||
            element.closest('aside')
        );
    };

    // ── Username Detection ───────────────────────────────────────────────────

    var findCurrentChatUsername = function () {
        var chatHeader = document.querySelector('[data-e2e="chat-header"]') ||
            document.querySelector('[data-e2e="chat-room-header"]') ||
            document.querySelector('[class*="ChatHeader"]') ||
            document.querySelector('[class*="chatHeader"]') ||
            document.querySelector('[class*="DivChatHeader"]') ||
            document.querySelector('[class*="ChatRoomTitle"]') ||
            document.querySelector('[class*="ConversationHeader"]');

        if (chatHeader && !isInsideChatList(chatHeader)) {
            var headerLink = chatHeader.querySelector('a[href*="/@"]');
            if (headerLink) {
                var href = headerLink.getAttribute('href') || '';
                var match = href.match(/\/@([^\/\?#]+)/);
                if (match && match[1]) {
                    return match[1];
                }
            }
            var text = chatHeader.innerText || chatHeader.textContent || '';
            var textMatch = text.match(/@([a-zA-Z0-9_.]+)/);
            if (textMatch && textMatch[1]) {
                return textMatch[1];
            }
            log('[HEADER] ChatHeader found but no @username detected inside it');
        } else {
            log('[HEADER] No ChatHeader element found on page (or was inside list)');
        }

        // Secondary check: look specifically within the active chat room container (right pane)
        var chatRoom = document.querySelector('[data-e2e="chat-room"]') ||
            document.querySelector('[class*="ChatRoom"]') ||
            document.querySelector('[class*="ChatDetail"]') ||
            document.querySelector('[class*="chatDetail"]') ||
            document.querySelector('[class*="DivMessageListContainer"]');

        if (chatRoom && !isInsideChatList(chatRoom)) {
            var roomLinks = chatRoom.querySelectorAll('a[href*="/@"]');
            for (var i = 0; i < roomLinks.length; i++) {
                var rLink = roomLinks[i];
                if (isInsideChatList(rLink)) continue;
                var rHref = rLink.getAttribute('href') || '';
                var rMatch = rHref.match(/\/@([^\/\?#]+)/);
                if (rMatch && rMatch[1]) {
                    log('[HEADER] Found username in active chat room: ' + rMatch[1]);
                    return rMatch[1];
                }
            }
        }

        // NEVER scan the global document.querySelectorAll('a[href*="/@"]')!
        // That matches the inbox conversation list sidebar and falsely claims the top user's chat is already open.
        return '';
    };

    var isTargetUser = function (currentName) {
        if (!currentName || !userName) return false;
        var cleanTarget = userName.trim().replace(/^@/, '').toLowerCase();
        var cleanCurrent = currentName.trim().replace(/^@/, '').toLowerCase();
        if (isGroup) {
            // Substring match for group names (may contain emoji/extra text)
            return cleanCurrent.indexOf(cleanTarget) !== -1;
        }
        return cleanCurrent === cleanTarget;
    };

    // Get the header text for group matching (no profile link needed)
    var findCurrentChatName = function () {
        var header = document.querySelector('[data-e2e="chat-header"]') ||
            document.querySelector('[class*="ChatHeader"]') ||
            document.querySelector('[class*="chatHeader"]') ||
            document.querySelector('[class*="DivChatHeader"]');
        if (!header) {
            log('[HEADER] No chat header found');
            return '';
        }

        var headerText = (header.textContent || '').trim();
        var profileLink = header.querySelector('a[href*="/@"]');
        
        // Group-specific markers from detect_groups.js
        var hasGroupClass = !!header.querySelector('[class*="group"], [class*="Group"], [class*="member"], [class*="Member"]');
        var memberMatch = headerText.match(/(\d+)\s*member/i);
        var avatars = header.querySelectorAll('img');

        log('[HEADER] Detect: profileLink=' + !!profileLink + ', groupClass=' + hasGroupClass + ', memberMatch=' + !!memberMatch + ', avatars=' + avatars.length);

        if (profileLink) {
            log('[HEADER] Detected as DM (has profile link)');
            return '';
        }

        if (hasGroupClass || memberMatch || avatars.length > 1) {
            log('[HEADER] Detected as GROUP via markers: "' + headerText.substring(0, 30) + '"');
            return headerText;
        }

        // Conservative fallback: if there is NO profile link, we treat it as a potential group
        log('[HEADER] Treating as potential group (no profile link): "' + headerText.substring(0, 30) + '"');
        return headerText;
    };

    // ── Message Input & Sending ──────────────────────────────────────────────

    var findMessageInput = function () {
        var editor = document.querySelector('[class*="DraftEditor-editorContainer"] [contenteditable="true"]') ||
            document.querySelector('[class*="DraftEditor-root"] [contenteditable="true"]') ||
            document.querySelector('[data-e2e="message-input"] [contenteditable="true"]') ||
            document.querySelector('div[contenteditable="true"][role="textbox"]') ||
            document.querySelector('div[contenteditable="true"]') ||
            document.querySelector('[contenteditable="true"]');

        return editor;
    };

    var findDraftEditor = function (messageInput) {
        var key = Object.keys(messageInput).find(function (k) {
            return k.startsWith('__reactFiber$') || k.startsWith('__reactInternalInstance$');
        });

        if (!key) {
            log('[DRAFT] React fiber not found on input element');
            return null;
        }

        log('[DRAFT] Found React fiber key: ' + key.substring(0, 20) + '...');
        var fiber = messageInput[key];
        var current = fiber;
        var depth = 0;

        while (current) {
            depth++;
            if (current.stateNode && current.stateNode.editor) {
                log('[DRAFT] Found Draft.js editor at fiber depth ' + depth);
                return current.stateNode;
            }
            current = current.return;
        }

        log('[DRAFT] Draft editor instance not found (walked ' + depth + ' fiber nodes)');
        return null;
    };

    var typeMessage = function (messageInput, callback) {
        log('[TYPE] Starting typeMessage...');

        var draftEditor = findDraftEditor(messageInput);

        if (draftEditor) {
            log('[TYPE] Using Draft.js _onPaste method');

            draftEditor.focus();

            setTimeout(function () {
                var dataTransfer = new DataTransfer();
                dataTransfer.setData('text/plain', message);

                var pasteEvent = new ClipboardEvent('paste', {
                    bubbles: true,
                    cancelable: true,
                    clipboardData: dataTransfer
                });

                try {
                    draftEditor._onPaste(pasteEvent);
                    log('[TYPE] _onPaste called successfully');
                } catch (e) {
                    log('[TYPE] _onPaste error: ' + e.message);
                }

                setTimeout(function () {
                    var content = messageInput.textContent || '';
                    log('[TYPE] Content after _onPaste: "' + content.substring(0, 50) + '" (len=' + content.length + ')');
                    callback();
                }, 300);
            }, 200);
        } else {
            log('[TYPE] Draft.js not found, falling back to execCommand');

            messageInput.click();
            messageInput.focus();

            setTimeout(function () {
                var selection = window.getSelection();
                var range = document.createRange();
                range.selectNodeContents(messageInput);
                range.collapse(false);
                selection.removeAllRanges();
                selection.addRange(range);

                document.execCommand('insertText', false, message);
                var content = messageInput.textContent || '';
                log('[TYPE] Content after execCommand: "' + content.substring(0, 50) + '" (len=' + content.length + ')');

                setTimeout(callback, 300);
            }, 200);
        }
    };

    var sendMessage = function (messageInput) {
        var sendBtn = document.querySelector('[data-e2e*="send"]') ||
            document.querySelector('[data-e2e*="Send"]') ||
            document.querySelector('button[type="submit"]');

        if (sendBtn) {
            log('[SEND] Found send button, clicking...');
            sendBtn.dispatchEvent(new Event('click', { bubbles: true }));
            return;
        }

        log('[SEND] No send button found, pressing Enter...');
        messageInput.dispatchEvent(new KeyboardEvent('keydown', {
            key: 'Enter',
            code: 'Enter',
            keyCode: 13,
            which: 13,
            bubbles: true,
            cancelable: true
        }));

        messageInput.dispatchEvent(new KeyboardEvent('keyup', {
            key: 'Enter',
            code: 'Enter',
            keyCode: 13,
            which: 13,
            bubbles: true
        }));
    };

    // ── Streak Detection ──────────────────────────────────────────────────────

    var extractThreadStreak = function () {
        try {
            // 1. Dedicated streak badges or aria-label markings
            var streakNodes = document.querySelectorAll(
                '[data-e2e*="streak"], [class*="streak"], [class*="Streak"], [aria-label*="streak" i], [aria-label*="Streak" i]'
            );

            for (var i = 0; i < streakNodes.length; i++) {
                var el = streakNodes[i];
                var raw = el.textContent || el.getAttribute('aria-label') || '';
                var match = raw.match(/(\d+)/);
                if (match) {
                    var val = parseInt(match[1], 10);
                    if (!isNaN(val) && val > 0 && val < 5000) {
                        log('[STREAK] Found badge streak: ' + val);
                        return val;
                    }
                }
            }

            // 2. Chat room header text or fire emoji patterns
            var header = document.querySelector('[data-e2e="chat-room-header"], [data-e2e*="chat-header"], header, [class*="ChatHeader"], [class*="chat-header"]');
            if (header) {
                var headerText = header.innerText || header.textContent || '';
                var flameMatch = headerText.match(/(?:🔥|streak|streaks)\s*(\d+)/i) || headerText.match(/(\d+)\s*(?:🔥|streak|streaks)/i);
                if (flameMatch) {
                    var fVal = parseInt(flameMatch[1], 10);
                    if (!isNaN(fVal) && fVal > 0 && fVal < 5000) {
                        log('[STREAK] Found header text streak: ' + fVal);
                        return fVal;
                    }
                }

                var spans = header.querySelectorAll('span, div');
                for (var j = 0; j < spans.length; j++) {
                    var span = spans[j];
                    if (span.children.length === 0) {
                        var text = span.textContent.trim();
                        if (/^\d{1,4}$/.test(text)) {
                            var sVal = parseInt(text, 10);
                            if (sVal > 0 && sVal < 5000) {
                                log('[STREAK] Found header digit streak: ' + sVal);
                                return sVal;
                            }
                        }
                    }
                }
            }
        } catch (e) {
            log('[STREAK] Extraction error: ' + e);
        }
        return 0;
    };

    // ── Result Reporting ─────────────────────────────────────────────────────

    var reportSuccess = function () {
        var streak = extractThreadStreak();
        log('[RESULT] SUCCESS — Message sent to ' + userName + (streak > 0 ? (' (Streak: ' + streak + ')') : ''));
        if (typeof StreakApp !== 'undefined') {
            if (typeof StreakApp.onMessageSentWithStreak === 'function') {
                StreakApp.onMessageSentWithStreak(userName, true, '', streak);
            } else {
                StreakApp.onMessageSent(userName, true, '');
            }
        }
    };

    var reportError = function (errorMessage) {
        log('[RESULT] FAIL — ' + errorMessage);
        if (typeof StreakApp !== 'undefined') {
            StreakApp.onMessageSent(userName, false, errorMessage);
        }
    };

    // ── Chat Processing ──────────────────────────────────────────────────────

    var sendMessageViaButton = function (attempt) {
        attempt = attempt || 1;
        var messageInput = findMessageInput();

        if (messageInput) {
            log('[CHAT] Found message input, typing...');
            typeMessage(messageInput, function () {
                sendMessage(messageInput);
                var jitterMs = 700 + Math.floor(Math.random() * 300);
                setTimeout(reportSuccess, jitterMs);
            });
        } else if (attempt <= 4) {
            log('[CHAT] Message input not yet available, polling (attempt ' + attempt + '/4)...');
            setTimeout(function () {
                sendMessageViaButton(attempt + 1);
            }, 400);
        } else {
            log('[CHAT] Message input NOT found on page after polling');
            reportError('Message input not found');
        }
    };

    var searchForUserInCurrentChat = function () {
        var currentName;
        if (isGroup) {
            currentName = findCurrentChatName();
            log('[CHAT] Current chat: "' + (currentName || '(DM)').substring(0, 50) + '" (group target: "' + userName + '")');
        } else {
            currentName = findCurrentChatUsername();
            log('[CHAT] Current chat username: "' + currentName + '" (target: "' + userName + '")');
        }

        if (currentName) {
            checkedUsernames[currentName.toLowerCase()] = true;
        }

        if (isTargetUser(currentName)) {
            found = true;
            log('[CHAT] MATCH — Found ' + (isGroup ? 'group' : 'target user'));
            sendMessageViaButton();
            return true;
        }

        return false;
    };

    var checkNextChat = function () {
        if (found || chatIndex >= chatItems.length) {
            if (!found) {
                log('[CHAT] Exhausted ' + chatItems.length + ' visible items (' + newItemsFound + ' new), trying scroll...');
                scrollAndRetry();
            }
            return;
        }

        var chatItem = chatItems[chatIndex];

        // Optimization: Detect type from list item before clicking to avoid unnecessary loads
        var itemAvatars = chatItem.querySelectorAll('img');
        var itemLinks = chatItem.querySelectorAll('a[href*="/@"]');

        if (isGroup) {
            // If searching for a group, but the list item has a clear profile link, it's a DM
            if (itemLinks.length > 0) {
                log('[CHAT] Item ' + (chatIndex + 1) + ' has profile link, skipping DM...');
                chatIndex++;
                setTimeout(checkNextChat, 10);
                return;
            }
        } else {
            // DM mode pre-click optimization (link check + handle text check)
            var matchedPreClick = false;
            for (var i = 0; i < itemLinks.length; i++) {
                var href = itemLinks[i].getAttribute('href') || '';
                var match = href.match(/\/@([^\/\?#]+)/);
                if (match && match[1]) {
                    var inlineUsername = match[1].toLowerCase().trim();
                    if (checkedUsernames[inlineUsername]) {
                        chatIndex++;
                        setTimeout(checkNextChat, 10);
                        return;
                    }
                    if (isTargetUser(match[1])) {
                        log('[CHAT] MATCH — Found @' + match[1] + ' in item ' + (chatIndex + 1) + ' (pre-click link)');
                        matchedPreClick = true;
                        break;
                    }
                }
            }

            if (!matchedPreClick) {
                var itemText = chatItem.innerText || chatItem.textContent || '';
                var textMatch = itemText.match(/@([a-zA-Z0-9_.]+)/);
                if (textMatch && textMatch[1] && isTargetUser(textMatch[1])) {
                    log('[CHAT] MATCH — Found @' + textMatch[1] + ' in item ' + (chatIndex + 1) + ' (pre-click text)');
                    matchedPreClick = true;
                }
            }

            if (matchedPreClick) {
                chatItem.click();
                found = true;
                setTimeout(sendMessageViaButton, 1000);
                return;
            }
        }

        log('[CHAT] Clicking item ' + (chatIndex + 1) + '/' + chatItems.length);
        chatItem.click();

        setTimeout(function () {
            var currentName;
            if (isGroup) {
                // Group mode: get header text, skip DMs (which have profile links)
                currentName = findCurrentChatName();
                if (currentName) {
                    log('[CHAT] Current chat: "' + currentName.substring(0, 50) + '" (group target: "' + userName + '")');
                } else {
                    // Has a profile link — this is a DM, skip it
                    var dmUser = findCurrentChatUsername();
                    log('[CHAT] Skipping DM: @' + dmUser);
                    if (dmUser) checkedUsernames[dmUser.toLowerCase()] = true;
                    chatIndex++;
                    checkNextChat();
                    return;
                }
            } else {
                // DM mode: get username from header
                currentName = findCurrentChatUsername();
                log('[CHAT] Current chat username: "' + currentName + '" (target: "' + userName + '")');
            }

            var nameKey = currentName ? currentName.toLowerCase().trim() : '';

            if (nameKey && checkedUsernames[nameKey]) {
                chatIndex++;
                checkNextChat();
                return;
            }

            if (nameKey) {
                checkedUsernames[nameKey] = true;
                newItemsFound++;
            }

            if (isTargetUser(currentName)) {
                found = true;
                log('[CHAT] MATCH — Found ' + (isGroup ? 'group' : 'target user') + ': "' + currentName + '"');
                sendMessageViaButton();
                return;
            }

            chatIndex++;
            checkNextChat();
        }, 1200);
    };

    // ── Initialization with Checkpoint Retries ───────────────────────────────

    var init = function () {
        try {
            if (userName.startsWith('@')) {
                userName = userName.substring(1);
            }

            log('[INIT] Starting automation initialization...');
            dumpEnvironment();
            log('[INIT] Target user: ' + userName);

            // Pre-check: only consider target chat already open if:
            // 1. A valid message input box exists on the page
            // 2. AND the header matches the target user/group
            var activeInput = findMessageInput();
            var preCheckName = isGroup ? findCurrentChatName() : findCurrentChatUsername();
            if (activeInput && preCheckName && isTargetUser(preCheckName)) {
                log('[INIT] Target ' + (isGroup ? 'group' : 'chat') + ' already open with active input: ' + preCheckName);
                found = true;
                setTimeout(sendMessageViaButton, 400);
                return;
            } else if (preCheckName && isTargetUser(preCheckName)) {
                log('[INIT] Target detected in header ("' + preCheckName + '") but message input not present. Proceeding with chat list selection for reliable focus.');
            }

            // Fast start: if chat items are already rendered in DOM, start immediately
            chatItems = findChatItems();
            if (chatItems.length > 0) {
                log('[INIT] Found ' + chatItems.length + ' chat items immediately, starting scan');
                checkNextChat();
                return;
            }

            log('[INIT] Waiting 2s for page stabilization...');
            setTimeout(function () {
                // ── Checkpoint: 2 seconds ──
                log('[CHECKPOINT-2S] Checking for chat items (attempt 1)...');
                chatItems = findChatItems();
                log('[CHECKPOINT-2S] Found ' + chatItems.length + ' chat items');

                if (chatItems.length > 0) {
                    log('[CHECKPOINT-2S] Items found, starting chat scan');
                    checkNextChat();
                    return;
                }

                // Zero items — run diagnostics and retry
                log('[CHECKPOINT-2S] Zero items found, running full diagnostics...');
                dumpFullDiagnostics();
                log('[CHECKPOINT-2S] Scheduling retry in 5 more seconds...');

                setTimeout(function () {
                    // ── Checkpoint: 7 seconds total ──
                    log('[CHECKPOINT-7S] Checking for chat items (attempt 2 after 7s total)...');
                    chatItems = findChatItems();
                    log('[CHECKPOINT-7S] Found ' + chatItems.length + ' chat items');

                    if (chatItems.length > 0) {
                        log('[CHECKPOINT-7S] Items found on retry, starting chat scan');
                        checkNextChat();
                        return;
                    }

                    log('[CHECKPOINT-7S] Still zero items after wait, running full diagnostics again...');
                    dumpFullDiagnostics();
                    log('[CHECKPOINT-7S] Giving up - reporting error');
                    reportError('No chat items found after wait');

                }, 5000);

            }, 2000);

        } catch (e) {
            log('[INIT] EXCEPTION: ' + e.message);
            reportError('Error: ' + e.message);
        }
    };

    // Start the automation
    init();
})();