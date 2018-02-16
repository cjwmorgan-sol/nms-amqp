All files located under the <project_root_dir>\test\config\cert contain example files to configure a secure client-server connection with certificate authentication. The following described files were all generated using the openssl tool and the java keytool. This files contains useful information about the example files.

Example Test Suite certificates.

ca.crt is the self signed certificate authority certificate. This certificate has a common name of "Test nms req".
ca.key is the private key for the ca.crt file.

ia.crt is the server certificate file signed by the ca.crt authority. This certificate has a common name of "NMS Test".
ia.key is the unencrypted private for the ia.crt file.

client.crt is an example client certificate file used by the Test Suite. This certificate has a common name of "NMS test Client".
client.key is the unencrypted private for the client.crt file.

KeyStore files.

identity.p12 is the key store that contains the server identity files; ia.crt and ia.key. The password to the key store is "password".
identity.jks is java keystore that contains the server identity files; ia.crt and ia.key. The password to the key store is "password".

client_trust.jks is the keystore that a broker would use to trust a client certificate. This store contains the certificate client.crt. This store has the password "password".

