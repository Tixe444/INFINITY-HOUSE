# Store Compliance Checklist
## INFINITY HOUSE - Made by Mate Makovics

This checklist ensures compliance with Apple App Store, Google Play Store, and Steam requirements.

---

## ✅ Apple App Store Requirements

### App Review Guidelines

- [ ] **Age Rating**: Set to appropriate IARC/ESRB rating
- [ ] **In-App Purchases**:
  - [ ] All IAP products use StoreKit
  - [ ] Receipt validation implemented (server-side)
  - [ ] Restore Purchases functionality working
  - [ ] No direct links to external payment systems
- [ ] **Loot Boxes/Random Items**:
  - [ ] Odds disclosure for all capsules/chests
  - [ ] Drop rates clearly displayed before purchase
  - [ ] Complies with App Review Guideline 3.1.1
- [ ] **Privacy**:
  - [ ] Privacy policy linked in app and App Store listing
  - [ ] ATT (App Tracking Transparency) prompt if tracking users
  - [ ] Analytics consent prompt implemented
- [ ] **Ask to Buy Support**:
  - [ ] Deferred purchase handling implemented
  - [ ] Don't grant items until purchase confirmed
- [ ] **Subscription Auto-Renewal** (if applicable):
  - [ ] Clear disclosure of renewal terms
  - [ ] Cancellation instructions provided

### Technical Requirements

- [ ] **Universal Build**:
  - [ ] Supports iPhone and iPad
  - [ ] Landscape and portrait orientations handled
- [ ] **Safe Area**:
  - [ ] UI respects iPhone notch/Dynamic Island
  - [ ] Tested on iPhone 14 Pro/15 Pro
- [ ] **Performance**:
  - [ ] 60 FPS on iPhone 8+
  - [ ] No crashes or memory leaks
  - [ ] Battery drain optimized
- [ ] **Network Loss**:
  - [ ] Graceful handling of connection loss
  - [ ] IAP state persists across disconnects

---

## ✅ Google Play Store Requirements

### Play Store Policies

- [ ] **In-App Purchases**:
  - [ ] Uses Google Play Billing Library v6+
  - [ ] Receipt verification implemented
  - [ ] Pending transactions handled
  - [ ] Deferred promo codes supported
- [ ] **Loot Boxes/Paid Random Items**:
  - [ ] Odds disclosure before purchase
  - [ ] Complies with Play Fair Consumer Policy
- [ ] **Privacy & Data Safety**:
  - [ ] Data Safety form completed accurately
  - [ ] GDPR compliance for EU users
  - [ ] COPPA compliance if under-13 users
- [ ] **Age Rating**:
  - [ ] IARC questionnaire completed
  - [ ] Rating certificate obtained

### Technical Requirements

- [ ] **APK/AAB Format**:
  - [ ] Android App Bundle (AAB) uploaded
  - [ ] 64-bit support enabled
- [ ] **Target API Level**:
  - [ ] Targets Android API 33+ (Android 13)
- [ ] **Performance**:
  - [ ] 60 FPS on mid-range devices (Galaxy S9+)
  - [ ] Optimized for 2GB RAM devices
- [ ] **Network Loss**:
  - [ ] Graceful offline handling
  - [ ] Purchase state persists

---

## ✅ Steam Requirements

### Steamworks Integration

- [ ] **Microtransactions**:
  - [ ] Steam Inventory Service integrated
  - [ ] Community Market support enabled
  - [ ] Receipt validation via Steam Web API
- [ ] **Steam Cloud**:
  - [ ] Save data synced to Steam Cloud
  - [ ] Cross-device progression working
- [ ] **Steam Input**:
  - [ ] Controller support (Xbox, PS, Steam Deck)
  - [ ] Controller glyphs displayed correctly
  - [ ] Keyboard remapping supported
- [ ] **Steam Deck**:
  - [ ] Verified/Playable on Steam Deck
  - [ ] 1280×800 UI legibility
  - [ ] 40-60 FPS performance
- [ ] **Achievements**:
  - [ ] Steam Achievements implemented
  - [ ] Achievement icons 64×64 and 32×32

### Store Page Requirements

- [ ] **Screenshots**:
  - [ ] Minimum 5 screenshots
  - [ ] 1920×1080 or higher resolution
- [ ] **Videos**:
  - [ ] At least one gameplay trailer
  - [ ] Max 2 minutes recommended
- [ ] **Age Rating**:
  - [ ] IARC rating applied
- [ ] **Store Description**:
  - [ ] Clear game description
  - [ ] System requirements accurate
  - [ ] No misleading claims

---

## ✅ Cross-Platform IAP Compliance

### Receipt Verification

- [ ] **iOS**:
  - [ ] Server-side verification with Apple
  - [ ] Test in Sandbox environment
  - [ ] Production receipt validation live
- [ ] **Android**:
  - [ ] Google Play Billing receipt verification
  - [ ] Server-side validation implemented
  - [ ] Subscription status checking
- [ ] **Steam**:
  - [ ] Steam Web API receipt validation
  - [ ] Transaction verification working

### Refunds & Support

- [ ] **Refund Policy**:
  - [ ] Clear refund instructions in-app
  - [ ] Link to platform-specific refund pages
- [ ] **Customer Support**:
  - [ ] Support email visible in app
  - [ ] Response within 48 hours
- [ ] **Help & FAQ**:
  - [ ] In-app help section
  - [ ] Common issues documented

---

## ✅ Monetization Ethics & Fairness

### Fair Monetization

- [ ] **Cosmetics Only**:
  - [ ] No pay-to-win mechanics
  - [ ] All gameplay content accessible for free
  - [ ] Cosmetics don't affect balance
- [ ] **Transparent Pricing**:
  - [ ] Prices clearly displayed
  - [ ] No hidden costs
  - [ ] Currency conversion visible
- [ ] **No Dark Patterns**:
  - [ ] No forced ads
  - [ ] No bait-and-switch pricing
  - [ ] No pressured purchases
- [ ] **Accessibility**:
  - [ ] Game fully playable without purchases
  - [ ] Free progression path viable

### Loot Box/Capsule Compliance

- [ ] **Odds Disclosure**:
  - [ ] Drop rates visible BEFORE purchase
  - [ ] Odds displayed as percentages
  - [ ] Pity system documented
- [ ] **No Misleading Odds**:
  - [ ] Actual drop rates match disclosed rates
  - [ ] Server-time used (no time manipulation)
  - [ ] RemoteConfig for rate adjustments

---

## ✅ Privacy & GDPR Compliance

### User Privacy

- [ ] **Privacy Policy**:
  - [ ] Hosted at accessible URL
  - [ ] Updated within last 12 months
  - [ ] Describes data collected
- [ ] **Analytics Consent**:
  - [ ] Opt-in/opt-out toggle in settings
  - [ ] Default respects regional laws
  - [ ] No tracking without consent
- [ ] **Data Deletion**:
  - [ ] User can delete account/data
  - [ ] Clear instructions provided
- [ ] **COPPA (if under-13)**:
  - [ ] Parental gate implemented
  - [ ] No behavioral advertising to children
  - [ ] Verifiable parental consent

---

## ✅ Testing Checklist

### Functional Testing

- [ ] **Purchase Flow**:
  - [ ] All IAP products purchasable
  - [ ] Receipt validation working
  - [ ] Entitlements granted correctly
- [ ] **Restore Purchases**:
  - [ ] iOS restore working
  - [ ] Android owned items detected
  - [ ] Steam inventory synced
- [ ] **Network Scenarios**:
  - [ ] Purchase with poor connection
  - [ ] Retry logic working
  - [ ] No double-charging
- [ ] **Device Time Spoofing**:
  - [ ] Server-time used for offers
  - [ ] Timers can't be cheated
  - [ ] Timed offers expire correctly

### Platform-Specific Testing

- [ ] **iOS**:
  - [ ] Tested on Sandbox
  - [ ] Tested on Production
  - [ ] All device sizes (SE, Pro Max, iPad)
- [ ] **Android**:
  - [ ] Tested on Google Play Test Track
  - [ ] Multiple device manufacturers (Samsung, Pixel)
  - [ ] Various Android versions (10-14)
- [ ] **Steam**:
  - [ ] Tested with Steam client
  - [ ] Controller input working
  - [ ] Steam Deck verified

---

## ✅ Launch Readiness

### Pre-Launch

- [ ] **Legal Review**:
  - [ ] Terms of Service finalized
  - [ ] Privacy Policy reviewed
  - [ ] Age ratings obtained
- [ ] **Backend Infrastructure**:
  - [ ] Receipt verification server live
  - [ ] Analytics endpoint operational
  - [ ] RemoteConfig deployed
- [ ] **Customer Support**:
  - [ ] Support email active
  - [ ] FAQ page published
  - [ ] Ticket system ready

### Post-Launch Monitoring

- [ ] **Metrics Dashboard**:
  - [ ] Purchase funnel tracked
  - [ ] Conversion rates monitored
  - [ ] Error rates < 1%
- [ ] **Store Ratings**:
  - [ ] Monitor reviews daily
  - [ ] Respond to negative feedback
  - [ ] Address reported bugs
- [ ] **Compliance Audits**:
  - [ ] Monthly compliance review
  - [ ] Policy updates monitored
  - [ ] Proactive fixes applied

---

## 📋 Final Approval

**Project**: INFINITY HOUSE
**Developer**: Mate Makovics
**Version**: 1.0.0
**Date**: _____________

### Sign-Off

- [ ] All checklist items completed
- [ ] Test results documented
- [ ] Compliance verified
- [ ] Ready for submission

**Approved by**: _____________
**Date**: _____________

---

## 📞 Support Contacts

- **Email**: support@infinityhouse.game
- **Website**: https://infinityhouse.game
- **Discord**: https://discord.gg/infinityhouse

**Last Updated**: 2025-01-03
