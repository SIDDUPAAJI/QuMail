# QuMailClient

A robust C#-based mail client designed for efficient communication and streamlined email management.

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![Visual Studio](https://img.shields.io/badge/Visual_Studio-5C2D91?style=for-the-badge&logo=visual-studio&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![GitHub](https://img.shields.io/badge/GitHub-181717?style=for-the-badge&logo=github&logoColor=white)

---

## Overview
QuMail represents a significant shift from "computational security" (which relies on math problems that quantum computers could solve) to "information-theoretic security" (which is mathematically unbreakable).By integrating Quantum Key Distribution (QKD) into a standard desktop email workflow, you are essentially "tunneling" quantum-safe data through today's classical internet.Core Architecture and ComponentsThe strength of QuMail lies in its ability to separate key generation from data transmission.1. The Simulated Key Manager (KM) & ETSI GS QKD 014In a real-world QKD network, physical hardware uses photons to create shared secret keys. Since QuMail is a desktop application, it interacts with a Key Manager (KM).The Protocol: By following the ETSI GS QKD 014 standard, your application uses a standardized REST-based API to request keys. This makes QuMail "hardware agnostic"—it doesn't care how the keys were made, only that it can fetch them securely.The Key Store: The KM ensures that both the sender (Alice) and the receiver (Bob) have identical copies of a unique Key ID, which is never transmitted over the open internet.2. Encryption Strategies: AES vs. OTPQuMail offers a tiered approach to security depending on the volume of keys available:One-Time Pad (OTP): If the QKD link produces enough key material, you can use OTP. This is the "holy grail" of cryptography where the key is as long as the message itself. It is unconditionally secure, meaning no amount of computing power (quantum or otherwise) can crack it.AES-256: If the message is large and key material is limited, the QKD keys are used as "session keys" for AES. While AES is classical, using a 256-bit key generated via QKD makes it extremely resistant to quantum attacks like Grover's Algorithm.The Communication WorkflowQuMail operates as a "wrapper" around the standard email process. Here is how a message moves from Alice to Bob:Key Retrieval: Alice’s QuMail client pings the local KM to get a $Key\_ID$ and the associated $Key\_Value$.Encryption: The plaintext email is encrypted locally on Alice’s machine using that $Key\_Value$.Encapsulation: The encrypted ciphertext and the $Key\_ID$ (but not the key itself) are packaged into a standard email format.Classical Transmission: The email is sent via standard protocols (SMTP). It travels through Gmail, Outlook, or private servers just like any other email.Decryption: Bob’s QuMail client receives the email, sees the $Key\_ID$, fetches the matching $Key\_Value$ from his local KM, and decrypts the message.Why This Matters: The "Harvest Now, Decrypt Later" ThreatCurrent email encryption (like PGP or S/MIME) relies on RSA or Elliptic Curve cryptography. An adversary can record your encrypted emails today and wait 10 years for a powerful quantum computer to decrypt them.QuMail solves this because:The keys used are not derived from mathematical "hard" problems.Even if an attacker intercepts the email today, there is no mathematical relationship for a future quantum computer to exploit, especially if OTP is used.Implementation StrengthsInfrastructure Compatibility: You don't need to rebuild the internet or change how IMAP/SMTP servers work. QuMail treats the email server as a simple "bit pipe."Quantum-Safe Transition: It provides a realistic bridge for enterprises to start using QKD hardware for high-stakes communication (legal, governmental, or financial) using a familiar interface.

## Tech Stack
* **Language:** C#
* **IDE:** Visual Studio 2022
* **Framework:** .NET
* **Version Control:** Git and GitHub


1. Programming Libraries for ETSI GS QKD 014Since the ETSI GS QKD 014 standard is built on RESTful APIs (HTTPS + JSON), implementation is very flexible. You aren't "doing quantum physics" in code; you are building a secure client-server relationship.Python: The "Rapid Prototyping" ChoicePython is the standard for QKD simulations because of its extensive cryptographic support.etsi-qkd-014-client: A specialized library that provides a ready-to-use client class to request keys, check status, and handle the mTLS (Mutual TLS) authentication required by the standard.Requests & Flask/FastAPI: If you are building the Simulated KM yourself, you would use FastAPI to create the REST endpoints (e.g., /api/v1/keys/{target_SAE_ID}/enc_keys).Cryptography (PyCA): Used to actually perform the AES-256 encryption once the quantum key is retrieved from the KM.C#: The "Enterprise Desktop" ChoiceIf you are building the QuMail desktop client for Windows, C# is often the preferred language.HttpClient with X509Certificate2: The ETSI standard requires mTLS. In C#, you use these classes to ensure the QuMail app and the KM trust each other via digital certificates.Newtonsoft.Json: To parse the complex key-delivery responses from the KM.BouncyCastle: A powerful crypto library used in C# to handle the high-precision encryption needed for One-Time Pad or custom AES implementations.


2. Database Schema for the Simulated Key ManagerA simulated Key Manager doesn't just "store" keys; it must track their lifecycle, which "SAE" (Secure Application Entity) they belong to, and whether they've been used.

<img width="1024" height="1024" alt="Gemini_Generated_Image_13qrmr13qrmr13qr" src="https://github.com/user-attachments/assets/eda7dad9-eca2-4fa0-b9f6-648eb34d00f0" />


A. High-Level System Architecture
The diagram above illustrates how the QuMail client interacts with both the simulated Key Manager and the traditional Email Server.
Component Breakdown:
Secure Application Entity (SAE): This is the QuMail Desktop App. It handles the user interface, fetches quantum keys via mTLS, and performs the encryption/decryption.
Key Management Entity (KME): This is your Simulated Server. It maintains the key database and implements the ETSI GS QKD 014 REST API.
Quantum Channel (Simulated): A background process that "generates" entropy and populates the KeyStore table, mimicking the behavior of a physical QKD link between two KMEs.
Classical Channel: The standard internet path (SMTP/IMAP) used to send the encrypted .qmail package.

B. The Functional Data Flow
This is the "handshake" that occurs every time you hit "Send."
Request (SAE → KME): Alice’s client calls GET /api/v1/keys/Bob_SAE_ID/enc_keys.
Delivery (KME → SAE): The KME picks a key from the KeyStore, marks it as Consumed in the database, and returns a Key_ID and Key_Value to Alice.
Encryption (Local): QuMail encrypts the email body using the Key_Value.
Classical Send: The email is sent. The body contains the Ciphertext and the Key_ID in the header.
Retrieval (Receiver): Bob’s QuMail sees the Key_ID in the incoming email and calls POST /api/v1/keys/Alice_SAE_ID/dec_keys using that ID to get the matching Key_Value.

C. Database Implementation (The "Brain")
To make the architecture work, your Simulated KME needs a relational structure to handle the "Key Synchronization" between users.
Entity Relationship Overview
Nodes & SAEs: Defines who is allowed to ask for keys.
KeyStore: A pool of high-entropy random strings.
KeyStreams: A logical "pipe" between two users. If Alice and Bob are communicating, they share a specific StreamID.

D. Technology Stack Recommendation
If you are building this project today, here is the most efficient stack:
Backend (KME Server): Python with FastAPI. It is natively asynchronous, making it perfect for handling high-frequency key requests. Use SQLite or PostgreSQL for the database.
Frontend (QuMail Client): C# with WPF or WinUI 3. This gives you a professional desktop look and feel with deep access to the Windows Credential Manager for storing the mTLS certificates.
Encryption Engine: PyCryptodome (Python) or BouncyCastle (C#). These libraries allow you to implement the One-Time Pad by XORing the message bytes directly with the Quantum Key bytes.


3. The "Secret Sauce": Simulating the KeyIn a real QKD system, keys are generated by light particles. In your simulation, you would use a Cryptographically Secure Pseudo-Random Number Generator (CSPRNG).Generation: The KM generates a block of random bits (e.g., 256 bits for AES or 1MB for OTP).Mapping: It assigns a UUID (Key ID) to that block.Synchronization: The KM "simulates" the quantum channel by ensuring that when Alice asks for a key for "Bob," that same key is reserved in the database for Bob when he checks in later using that specific UUID.Why this Architecture is SecureEven if a hacker breaches your email server, they only see the KeyID (a random string like 8c3c8d07...) and the encrypted text. To get the KeyValue, they would have to breach your Key Manager, which is protected by a different set of credentials and potentially sits on a completely different network.

---
Developed by SIDDUPAAJI


